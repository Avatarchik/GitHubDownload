using System;
using System.IO;

using System.Net;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditorInternal;
using System.Threading.Tasks;

using static System.IO.Path;
using E = Hananoki.GitHubDownload.GitHubDownloadSettingsEditor;

namespace Hananoki.GitHubDownload {

	public class GitHubDownloadWindow : EditorWindow {

		string githubURL = "https://api.github.com/repos";

		//string[] urls = {
		//	"https://github.com/hananoki/HananokiSharedModule.git",
		//	"https://github.com/hananoki/HierarchyDropDown.git",
		//	};

		public int m_selectURL;
		public ReleaseJson m_js;

		public class Styles {
			public GUIStyle ExposablePopupItem;
			public GUIStyle boldLabel;
			public GUIStyle miniBoldLabel;
			public GUIStyle helpBox;

			public Texture2D Icon;
			public Texture2D IconSetting;
			public Texture2D IconError;
			public Texture2D IconInfo;
			public Texture2D[] IconWaitSpin;
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
			}
		}

		public static Styles s_styles;


		public string[] m_downloadFiles;
		static Vector2 m_scroll;

		bool indexChanged;
		bool networking;
		public string networkingMsg;

		public static string gitHubCacheDirectory {
			get {
#if UNITY_EDITOR_WIN
				return GetDirectoryName( ( GetDirectoryName( InternalEditorUtility.unityPreferencesFolder ) ) ).Replace( '\\', '/' ) + "/GitHub";
#elif UNITY_EDITOR_OSX
			return InternalEditorUtility.unityPreferencesFolder + "/" + "../../../Unity/GitHub";
#else
			return "";
#endif
			}
		}



		[MenuItem( "Window/GitHubDownload" )]
		public static void Open() {
			var window = GetWindow<GitHubDownloadWindow>();
			window.titleContent = new GUIContent( "GitHubDownload" );
		}


		void OnEnable() {
			E.Load();
			MakeDownloadList();
			ReadJson();
			indexChanged = true;

			
		}

		static float curTime;
		static float lastTime;
		public static int m_count;
		static float m_watiTime;

		public  void updateThreadSync() {
			curTime = Time.realtimeSinceStartup;
			float deltaTime = (float) ( curTime - lastTime );
			lastTime = curTime;

			m_watiTime -= deltaTime;
			if( m_watiTime < 0 ) {
				m_watiTime = 0.1250f;

				m_count++;
				if( 12 <= m_count ) {
					m_count = 0;
				}
				Repaint();
			}
		}


		string[] ParseURL( string gitURL ) {
			if( string.IsNullOrEmpty( gitURL ) ) return null;
			var m = Regex.Matches( gitURL, @"^(https://github.com)/(.*)" );
			string[] ss = m[ 0 ].Groups[ 2 ].Value.Split( '/' );

			return new string[] { ss[ 0 ], GetFileNameWithoutExtension( ss[ 1 ] ) };
		}



		string GetCurrentURL() {
			if( E.i.urls.Count == 0 ) return string.Empty;
			if( m_selectURL < 0 ) {
				m_selectURL = 0;
				indexChanged = true;
			}
			if( E.i.urls.Count <= m_selectURL  ) {
				m_selectURL = E.i.urls.Count -1;
				indexChanged = true;
			}
			return E.i.urls[ m_selectURL ];
		}


		void MakeDownloadList() {
			m_downloadFiles = new string[ 0 ];

			var info = ParseURL( GetCurrentURL() );
			var outputDirectory = $"{gitHubCacheDirectory}/{info[ 0 ]}/{info[ 1 ]}";

			if( !Directory.Exists( outputDirectory ) ) return;

			m_downloadFiles = Directory.GetDirectories( outputDirectory );
		}


		void SaveJson( ReleaseJson js ) {
			if( js == null ) return;
			var info = ParseURL( E.i.urls[ m_selectURL ] );
			var opath = $"{gitHubCacheDirectory}/{info[ 0 ]}/{info[ 1 ]}";

			if( !Directory.Exists( opath ) ) {
				Directory.CreateDirectory( opath );
			}

			using( var st = new StreamWriter( $"{opath}/releases_latest.json" ) ) {
				st.Write( JsonUtility.ToJson( js ) );
			}
		}


		void ReadJson() {
			m_js = null;
			var info = ParseURL( E.i.urls[ m_selectURL ] );
			var opath = $"{gitHubCacheDirectory}/{info[ 0 ]}/{info[ 1 ]}";

			if( !Directory.Exists( opath ) ) return;

			using( var st = new StreamReader( $"{opath}/releases_latest.json" ) ) {
				m_js = JsonUtility.FromJson<ReleaseJson>( st.ReadToEnd() );
			}
		}


		async void GetReleasesLatest( string name, string repoName ) {
			string content = string.Empty;
			m_js = null;
			networking = true;
			networkingMsg = "Download Release Latest Json";
			EditorApplication.update += updateThreadSync;
			await Task.Run( () => {
				using( var client = new WebClient() ) {
					var url = $"{githubURL}/{name}/{repoName}/releases/latest";

					client.Headers.Add( "User-Agent", "Nothing" );
					//System.Threading.Thread.Sleep( 10000 );
					content = client.DownloadString( url );
				}
			} );
			networking = false;
			EditorApplication.update -= updateThreadSync;
			m_js = JsonUtility.FromJson<ReleaseJson>( content );
			if( m_js == null ) return;
			SaveJson( m_js );
		}

		void GetReleasesLatest( string gitURL ) {
			var m = ParseURL( gitURL );
			GetReleasesLatest( m[ 0 ], m[ 1 ] );
		}


		public async void DownloadFile( string url, string name, string repoName, string tag ) {
			var outputDirectory = $"{gitHubCacheDirectory}/{name}/{repoName}/{tag}";

			if( !Directory.Exists( outputDirectory ) ) {
				Directory.CreateDirectory( outputDirectory );
			}

			networking = true;
			networkingMsg = "Download File";
			EditorApplication.update += updateThreadSync;
			await Task.Run( () => {
				var fname = GetFileName( url );
				//System.Threading.Thread.Sleep( 10000 );
				using( WebClient wc = new WebClient() ) {
					wc.DownloadFile( new Uri( url ), outputDirectory + "/" + fname );
				}
			} );
			networking = false;
			EditorApplication.update -= updateThreadSync;

			MakeDownloadList();
		}


		void DrawToolbar() {
			GUILayout.BeginHorizontal( EditorStyles.toolbar );
			if( GUILayout.Button( s_styles.IconSetting, EditorStyles.toolbarButton ) ) {
				EditorApplication.ExecuteMenuItem( "Edit/Preferences..." );
			}
			EditorGUI.BeginChangeCheck();
			if( E.i.urls.Count == 0 ) {
				//GUILayout.Label( new GUIContent( "Missing URL", s_styles.IconInfo ), EditorStyles.toolbarButton );
			}
			else {
				m_selectURL = EditorGUILayout.Popup( m_selectURL, E.i.urls.Select( x => GetFileNameWithoutExtension( x ) ).ToArray(), EditorStyles.toolbarDropDown );
			}

			if( EditorGUI.EndChangeCheck()  ) {
				MakeDownloadList();
				ReadJson();
				indexChanged = true;
			}
			GUILayout.FlexibleSpace();

			if( GUILayout.Button( "Get Releases Latest", EditorStyles.toolbarButton ) ) {
				GetReleasesLatest( GetCurrentURL() );
			}

			GUILayout.EndHorizontal();
		}


		void DrawGUI() {
			E.Load();

			if( s_styles == null ) {
				s_styles = new Styles();
				MakeDownloadList();
				ReadJson();
			}

			DrawToolbar();

			var info = ParseURL( GetCurrentURL() );

			if( info == null ) {
				EditorGUILayout.HelpBox( "Set URL from preferences", MessageType.Info );
				return;
			}

			
			using( new GUILayout.HorizontalScope() ) {
				bool force = false;
				if( indexChanged ) {
					if( m_js == null ) {
						indexChanged = false;
						force = true;
					}
				}
				if( /*GUILayout.Button( "Get Releases Latest" ) ||*/ force ) {
					Debug.Log( "Get Releases Latest" );
					
					GetReleasesLatest( GetCurrentURL() );
				}
			}

			using( var sc = new GUILayout.ScrollViewScope( m_scroll ) ) {
				m_scroll = sc.scrollPosition;

				if( m_js != null ) {
					GUILayout.Label( info[1], s_styles.boldLabel );
					var rc = GUILayoutUtility.GetLastRect();
					rc.x += s_styles.boldLabel.CalcSize( new GUIContent( info[1] ) ).x;
					rc.x += 8;
					GUI.Label( rc, m_js.tag_name, s_styles.miniBoldLabel );

					EditorGUILayout.LabelField( m_js.body, s_styles.helpBox );

					//var outputDirectory = $"{gitHubCacheDirectory}/{info[ 0 ]}/{info[ 1 ]}/{m_js.tag_name}";
					foreach( var asset in m_js.assets ) {
						var fname = GetFileName( asset.browser_download_url );
						if( GUILayout.Button( new GUIContent( fname, s_styles.Icon ), s_styles.ExposablePopupItem, GUILayout.ExpandWidth( false ) ) ) {
							DownloadFile( asset.browser_download_url, info[ 0 ], info[ 1 ], m_js.tag_name );
						}
					}

					if( !string.IsNullOrEmpty( m_js.zipball_url ) ) {
						if( GUILayout.Button( new GUIContent( "Source code (zip)", s_styles.Icon ), s_styles.ExposablePopupItem, GUILayout.ExpandWidth( false ) ) ) {
						}
					}
					if( !string.IsNullOrEmpty( m_js.tarball_url ) ) {
						if( GUILayout.Button( new GUIContent( "Source code (tar.gz)", s_styles.Icon ), s_styles.ExposablePopupItem, GUILayout.ExpandWidth( false ) ) ) {
						}
					}
				}


				foreach( var p in m_downloadFiles ) {
					if( GUILayout.Button( GetFileName( p ) ) ) {
						
						AssetDatabase.ImportPackage( Directory.GetFiles( p )[ 0 ] , true);
					}
				}
			}

			
			if( networking ) {
				var last = GUILayoutUtility.GetLastRect();
				var y = last.y + last.height - 20;
				last.y = y;
				last.height = 20;
				//EditorGUI.DrawRect( last, new Color( 0, 0, 1, 0.5f ) );
				var cont = new GUIContent( networkingMsg, s_styles.IconWaitSpin[ m_count ] );
				last.width = EditorStyles.label.CalcSize( cont ).x;
				last.x += 4;
				last.width += 4;
				last.y -= 4;
				EditorGUI.DrawRect( last, new Color( 1, 1, 1, 0.5f ) );
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

