
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

using static System.IO.Path;

using E = Hananoki.GitHubDownload.GitHubDownloadSettingsEditor;

namespace Hananoki.GitHubDownload {

	public class GitHubDownloadWindow : EditorWindow {

		public static GitHubDownloadWindow s_window;

		string githubURL = "https://api.github.com/repos";

		public int m_selectURL;
		public List<ReleaseJson> m_js;

		public class Styles {
			public GUIStyle ExposablePopupItem;
			public GUIStyle boldLabel;
			public GUIStyle miniBoldLabel;
			public GUIStyle helpBox;
			public GUIStyle toolbarDropDown;

			public Texture2D Icon;
			public Texture2D IconSetting;
			public Texture2D IconError;
			public Texture2D IconInfo;
			public Texture2D[] IconWaitSpin;
			public Texture2D IconRefresh;
			public Texture2D IconSceneAsset;
			public Styles() {
				ExposablePopupItem = new GUIStyle( "ExposablePopupItem" );
				ExposablePopupItem.margin = new RectOffset( 2, 2, 2, 2 );
				ExposablePopupItem.hover.textColor = new Color( 3f / 255f, 102f / 255f, 214f / 255f, 1 );
				ExposablePopupItem.fontStyle = FontStyle.Bold;
				boldLabel = new GUIStyle( EditorStyles.boldLabel );
				miniBoldLabel = new GUIStyle( EditorStyles.boldLabel );
				miniBoldLabel.fontSize = EditorStyles.miniBoldLabel.fontSize;
				helpBox = new GUIStyle( EditorStyles.helpBox );
				helpBox.fontSize = EditorStyles.boldLabel.fontSize;
				IconError = EditorGUIUtility.FindTexture( "console.erroricon.sml" );
				IconInfo = EditorGUIUtility.FindTexture( "console.infoicon.sml" );
				Icon = EditorGUIUtility.FindTexture( "winbtn_mac_inact" );
				IconWaitSpin = Resources.FindObjectsOfTypeAll<Texture2D>().Where( x => x.name.Contains( "WaitSpin" )).OrderBy( x => x.name ).ToArray();
				IconSetting = EditorGUIUtility.FindTexture( "SettingsIcon" );
				IconRefresh = EditorGUIUtility.FindTexture( "Refresh" );
				IconSceneAsset = Resources.FindObjectsOfTypeAll<Texture2D>().Where( x => x.name == "SceneAsset Icon" ).ToArray()[0];

				toolbarDropDown = new GUIStyle( EditorStyles.toolbarDropDown );
				toolbarDropDown.alignment = TextAnchor.MiddleCenter;
				toolbarDropDown.padding.right = 12;
			}
		}

		public static Styles s_styles;


		public List<string> m_downloadDirs;
		static Vector2 m_scroll;

		bool indexChanged;

		bool enableLatest;

		string[] showRelease = { "Releases", "Latest Releases" };
		


		[MenuItem( "Window/GitHubDownload" )]
		public static void Open() {
			var window = GetWindow<GitHubDownloadWindow>();
			window.titleContent = new GUIContent( "GitHubDownload" );
		}

		new public static void Repaint() {
			((EditorWindow) s_window )?.Repaint();
		}

		void OnEnable() {
			s_window = this;
			E.Load();
			MakeDownloadList();
			ReadWebResponseToFile( enableLatest );
			indexChanged = true;
		}


		string MakeOutputPath( string gitURL ) {
			var info = ParseURL( GetCurrentURL() );
			return $"{E.gitHubCacheDirectory}/{info[ 0 ]}/{info[ 1 ]}";
		}

		string[] ParseURL( string gitURL ) {
			if( string.IsNullOrEmpty( gitURL ) ) return null;
			var m = Regex.Matches( gitURL, @"^(https://github.com)/(.*)" );
			string[] ss = m[ 0 ].Groups[ 2 ].Value.Split( '/' );

			return new string[] { ss[ 0 ], GetFileNameWithoutExtension( ss[ 1 ] ) };
		}


		bool AdjustSelectURL() {
			if( E.i.urls.Count == 0 ) return false;
			if( m_selectURL < 0 ) {
				m_selectURL = 0;
				indexChanged = true;
				return true;
			}
			if( E.i.urls.Count <= m_selectURL ) {
				m_selectURL = E.i.urls.Count - 1;
				indexChanged = true;
				return true;
			}
			return false;
		}


		string GetCurrentURL() {
			if( E.i.urls.Count == 0 ) return string.Empty;

			AdjustSelectURL();

			return E.i.urls[ m_selectURL ];
		}


		void MakeDownloadList() {
			m_downloadDirs = new List<string>();

			var info = ParseURL( GetCurrentURL() );
			if( info == null ) return;
			var outputDirectory = $"{E.gitHubCacheDirectory}/{info[ 0 ]}/{info[ 1 ]}";

			if( !Directory.Exists( outputDirectory ) ) return;

			m_downloadDirs = Directory.GetDirectories( outputDirectory ).ToList();
		}


		void WriteWebResponseToFile( string content, bool latest ) {
			var opath = MakeOutputPath( GetCurrentURL() );

			if( !Directory.Exists( opath ) ) {
				Directory.CreateDirectory( opath );
			}

			if( latest ) {
				opath = $"{opath}/releases_latest.json";
			}
			else {
				opath = $"{opath}/releases.json";
			}

			

			using( var st = new StreamWriter( opath ) ) {
				st.Write(  content  );
			}
		}


		void ReadWebResponseToFile( bool latest ) {
			m_js = new List<ReleaseJson>();
			var info = ParseURL( GetCurrentURL() );
			if( info == null ) return;

			var opath = $"{E.gitHubCacheDirectory}/{info[ 0 ]}/{info[ 1 ]}";

			if( !Directory.Exists( opath ) ) return;

			if( latest ) {
				var fname = $"{opath}/releases_latest.json";
				if( !File.Exists( fname ) ) return;

				using( var st = new StreamReader( fname ) ) {
					var jss = JsonUtility.FromJson<ReleaseJson>( st.ReadToEnd() );
					m_js.Add( jss );
				}
			}
			else {
				var fname = $"{opath}/releases.json";
				if( !File.Exists( fname ) ) return;

				using( var st = new StreamReader( fname ) ) {
					var content = st.ReadToEnd();
					var content2 = "{\"Items\":" + content + "}";
					var jss = JsonHelper.FromJson<ReleaseJson>( content2 );
					m_js.AddRange( jss );
				}
			}
		}


		void GetReleasesResponse( string name, string repoName, bool latest ) {
			//string content = string.Empty;
			m_js = new List<ReleaseJson>();

			try {
				//using( new RequestStatusScope( latest ? "Downloading Release Latest ..." : "Downloading Release ..." ) )
				using( var wc = new WebClient() ) {
					var url = $"{githubURL}/{name}/{repoName}/releases";
					if( latest ) {
						url += "/latest";
					}
					wc.Headers.Add( "User-Agent", "Nothing" );
					wc.DownloadStringCompleted += ( sender, e ) => {
						var content = e.Result;
						if( string.IsNullOrEmpty( content ) ) return;

						if( latest ) {
							var rj = JsonUtility.FromJson<ReleaseJson>( content );
							if( rj == null ) return;
							m_js.Add( rj );
						}
						else {
							var content2 = "{\"Items\":" + content + "}";
							var jss = JsonHelper.FromJson<ReleaseJson>( content2 );
							m_js.AddRange( jss );
						}
						WriteWebResponseToFile( content, latest );
						Repaint();
					};
					wc.DownloadStringAsync( new Uri( url ) );

				}
			}
			catch( Exception e ) {
				Debug.LogException( e );
				//RequestStatus.SetError( e );
			}
		}


		void GetReleasesResponse( string gitURL, bool latest ) {
			var m = ParseURL( gitURL );
			GetReleasesResponse( m[ 0 ], m[ 1 ], latest );
		}


		public void DownloadFile( string url, string name, string repoName, string tag, string extention = "" ) {
			if( RequestStatus.networking ) {
				EditorUtility.DisplayDialog( "Warning", "No new downloads can be added during download","OK" );
				return;
			}
			var outputDirectory = $"{E.gitHubCacheDirectory}/{name}/{repoName}/{tag}";

			if( !Directory.Exists( outputDirectory ) ) {
				Directory.CreateDirectory( outputDirectory );
			}

			try {
				string fname;
				if( string.IsNullOrEmpty( extention ) ) {
					fname = GetFileName( url );
				}
				else {
					fname = $"{repoName}-{tag}{extention}";
				}
				//System.Threading.Thread.Sleep( 10000 );
				//using( new RequestStatusScope( "Download File " + GetFileName( url ) ) )

				using( WebClient wc = new WebClient() ) {
					RequestStatus.Begin( $"Download File {GetFileName( url )}" );
					wc.Headers.Add( "User-Agent", "Nothing" );
					wc.DownloadProgressChanged += ( sender, e ) => {
						//Debug.Log( "DownloadProgressChanged" );
						//RequestStatus.networkingMsg = $"Download File {GetFileName( url )} {e.BytesReceived}of {e.TotalBytesToReceive} bytes. {e.ProgressPercentage} % complete...";
						RequestStatus.networkingMsg = $"Download File {GetFileName( url )} {e.ProgressPercentage} %";
					};
					wc.DownloadFileCompleted += ( sender, e ) => {
						//Debug.Log( "DownloadFile" );
						RequestStatus.End();

						MakeDownloadList();
						Repaint();
					};
					wc.DownloadFileAsync( new Uri( url ), outputDirectory + "/" + fname );
				}
			}
			catch( Exception e ) {
				Debug.LogException( e );
				RequestStatus.SetError( e );
			}
		}




		void DrawToolbar() {
			GUILayout.BeginHorizontal( EditorStyles.toolbar );
			if( GUILayout.Button( s_styles.IconSetting, EditorStyles.toolbarButton ) ) {
				EditorApplication.ExecuteMenuItem( "Edit/Preferences..." );
			}
			EditorGUI.BeginChangeCheck();
			bool force = false;
			if( 0 < E.i.urls.Count ) {
				AdjustSelectURL();
				m_selectURL = EditorGUILayout.Popup( m_selectURL, E.i.urls.Select( x => GetFileNameWithoutExtension( x ) ).ToArray(), s_styles.toolbarDropDown );
			}
			E.i.showMode = EditorGUILayout.Popup( E.i.showMode, showRelease, s_styles.toolbarDropDown, GUILayout.Width( 120) );
			if( EditorGUI.EndChangeCheck() || force ) {
				enableLatest = E.i.showMode != 0;
				MakeDownloadList();
				ReadWebResponseToFile( enableLatest );
				indexChanged = true;
				RequestStatus.Reset();
			}
			if( GUILayout.Button( s_styles.IconRefresh, EditorStyles.toolbarButton ) ) {
				GetReleasesResponse( GetCurrentURL(), enableLatest );
			}

			GUILayout.FlexibleSpace();

			GUILayout.EndHorizontal();
		}


		void DrawGUI() {
			E.Load();

			if( s_styles == null ) {
				s_styles = new Styles();
				MakeDownloadList();
				ReadWebResponseToFile( enableLatest );
			}

			DrawToolbar();

			var info = ParseURL( GetCurrentURL() );

			if( info == null ) {
				EditorGUILayout.HelpBox( "Set URL from preferences", MessageType.Info );
				return;
			}
			if( RequestStatus.networkError  ) {
				EditorGUILayout.HelpBox( RequestStatus.networkingErrorMsg, MessageType.Error );
				return;
			}

			using( new GUILayout.HorizontalScope() ) {
				bool force = false;
				if( indexChanged ) {
					indexChanged = false;
					if( m_js == null || m_js.Count==0 ) {
						force = true;
					}
				}
				if( /*GUILayout.Button( "Get Releases Latest" ) ||*/ force ) {
					//Debug.Log( "Get Releases Latest" );
					
					GetReleasesResponse( GetCurrentURL(), enableLatest );
				}
			}

			using( var sc = new GUILayout.ScrollViewScope( m_scroll ) ) {
				if( m_js != null ) {
					m_scroll = sc.scrollPosition;
					if( m_js.Count == 0 ) {
						if( !RequestStatus.networking ) {
							EditorGUILayout.HelpBox( "No release", MessageType.Warning );
						}
					}
					else {
						foreach( var p in m_js ) {
							using( new GUILayout.VerticalScope( s_styles.helpBox ) ) {
								using( new GUILayout.HorizontalScope() ) {
									p.toggle = GUIHelper.Foldout( p.toggle, $"{p.tag_name}" );

									var dldirs = m_downloadDirs.Where( x => GetFileName( x ) == p.tag_name ).ToList();
									foreach( var dr in dldirs ) {
										foreach( var fname in Directory.GetFiles( dr ) ) {
											if( GetExtension( fname ) == ".unitypackage" ) {
												var cont = new GUIContent( s_styles.IconSceneAsset );
												var rc = GUILayoutUtility.GetRect( cont, GUIHelper.Styles.iconButton );
#if UNITY_2019_3_OR_NEWER
												rc.y += 1;
#endif
												if( GUIHelper.IconButton( rc, s_styles.IconSceneAsset ) ) {
													AssetDatabase.ImportPackage( fname, true );
												}
											}
										}
									}
								}

								if( !p.toggle ) continue;

								EditorGUILayout.LabelField( p.body, s_styles.helpBox );

								//var outputDirectory = $"{gitHubCacheDirectory}/{info[ 0 ]}/{info[ 1 ]}/{m_js.tag_name}";
								foreach( var asset in p.assets ) {
									var fname = GetFileName( asset.browser_download_url );
									if( GUILayout.Button( new GUIContent( fname, s_styles.Icon ), s_styles.ExposablePopupItem, GUILayout.ExpandWidth( false ) ) ) {
										DownloadFile( asset.browser_download_url, info[ 0 ], info[ 1 ], p.tag_name );
									}
								}

								if( !string.IsNullOrEmpty( p.zipball_url ) ) {
									if( GUILayout.Button( new GUIContent( "Source code (zip)", s_styles.Icon ), s_styles.ExposablePopupItem, GUILayout.ExpandWidth( false ) ) ) {
										DownloadFile( p.zipball_url, info[ 0 ], info[ 1 ], p.tag_name, ".zip" );
									}
								}
								if( !string.IsNullOrEmpty( p.tarball_url ) ) {
									if( GUILayout.Button( new GUIContent( "Source code (tar.gz)", s_styles.Icon ), s_styles.ExposablePopupItem, GUILayout.ExpandWidth( false ) ) ) {
										DownloadFile( p.tarball_url, info[ 0 ], info[ 1 ], p.tag_name, ".tar.gz" );
									}
								}
							}
						}
					}

				}
			}
			
			if( RequestStatus.networking ) {
				var last = GUILayoutUtility.GetLastRect();
				last.height = 20;
				
				var cont = new GUIContent( RequestStatus.networkingMsg, s_styles.IconWaitSpin[ RequestStatus.m_count ] );
				last.width = EditorStyles.label.CalcSize( cont ).x;
				last.x += 4;
				last.width += 4;
				last.y += 4;
				EditorGUI.DrawRect( last, new Color( 1, 1, 1, 1.0f ) );
				GUI.Label( last, cont );
			}
		}


		/// <summary>
		/// 
		/// </summary>
		void OnGUI() {
			try {
				DrawGUI();
			}
			catch( Exception e ) {
				Debug.LogException(e);
			}
		}
	}
}

