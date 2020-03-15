
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

using static System.IO.Path;

using E = Hananoki.GitHubDownload.GitHubDownloadSettingsEditor;

namespace Hananoki.GitHubDownload {

	public class GitHubDownloadWindow : EditorWindow {

		public static GitHubDownloadWindow s_window;



		public int m_selectURL;
		public List<ReleaseJson> m_js;

		public class Styles {
			public GUIStyle ExposablePopupItem;
			public GUIStyle boldLabel;
			public GUIStyle miniBoldLabel;
			public GUIStyle helpBox;
			public GUIStyle toolbarDropDown;

			public Texture2D IconPackage;
			public Texture2D IconIndicatorOFF;
			public Texture2D IconIndicatorON;
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

				IconPackage = EditorGUIUtility.FindTexture( "winbtn_mac_inact" );
				IconIndicatorOFF = EditorGUIUtility.FindTexture( "lightMeter/lightRim" );
				IconIndicatorON = EditorGUIUtility.FindTexture( "lightMeter/greenLight" );

				IconWaitSpin = Resources.FindObjectsOfTypeAll<Texture2D>().Where( x => x.name.Contains( "WaitSpin" ) ).OrderBy( x => x.name ).ToArray();
				IconSetting = EditorGUIUtility.FindTexture( "SettingsIcon" );
				IconRefresh = EditorGUIUtility.FindTexture( "Refresh" );
				IconSceneAsset = Resources.FindObjectsOfTypeAll<Texture2D>().Where( x => x.name == "SceneAsset Icon" ).ToArray()[ 0 ];

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
			( (EditorWindow) s_window )?.Repaint();
		}


		void OnEnable() {
			s_window = this;
			E.Load();
			MakeDownloadList();
			ReadWebResponseToFile( enableLatest );
			indexChanged = true;
		}




		bool AdjustSelectURL() {
			if( E.i.gitUrls.Count == 0 ) return false;
			if( m_selectURL < 0 ) {
				m_selectURL = 0;
				indexChanged = true;
				return true;
			}
			if( E.i.gitUrls.Count <= m_selectURL ) {
				m_selectURL = E.i.gitUrls.Count - 1;
				indexChanged = true;
				return true;
			}
			return false;
		}



		string MakeOutputPath( string gitURL ) {
			var info = Helper.ParseURL( GetCurrentURL() );
			return $"{E.gitHubCacheDirectory}/{info[ 0 ]}/{info[ 1 ]}";
		}


		string GetCurrentURL() {
			if( E.i.gitUrls.Count == 0 ) return string.Empty;

			AdjustSelectURL();

			return E.i.gitUrls[ m_selectURL ].url;
		}


		void MakeDownloadList() {
			m_downloadDirs = new List<string>();

			var info = Helper.ParseURL( GetCurrentURL() );
			if( info == null ) return;
			var outputDirectory = $"{E.gitHubCacheDirectory}/{info[ 0 ]}/{info[ 1 ]}";

			if( !Directory.Exists( outputDirectory ) ) return;

			m_downloadDirs = Directory.GetDirectories( outputDirectory ).ToList();
		}





		void ReadWebResponseToFile( bool latest ) {
			var info = Helper.ParseURL( GetCurrentURL() );
			if( info == null ) return;

			m_js = new List<ReleaseJson>();

			if( latest ) {
				Helper.ReadWebResponseToFile( info[ 0 ], info[ 1 ], "releases/latest", ( content ) => {
					var jss = JsonUtility.FromJson<ReleaseJson>( content );
					m_js.Add( jss );
				} );
			}
			else {
				Helper.ReadWebResponseToFile( info[ 0 ], info[ 1 ], "releases", ( content ) => {
					var content2 = "{\"Items\":" + content + "}";
					var jss = JsonHelper.FromJson<ReleaseJson>( content2 );
					m_js.AddRange( jss );
				} );
			}
		}


		void GetReleasesResponse( string gitURL, bool latest ) {
			GetTagsResponse( gitURL );

			m_js = new List<ReleaseJson>();
			var m = Helper.ParseURL( gitURL );
			if( latest ) {
				Helper.GetResponse( m[ 0 ], m[ 1 ], "releases/latest", ( content ) => {
					var js = JsonUtility.FromJson<ReleaseJson>( content );
					if( js == null ) return;
					m_js.Add( js );
					Repaint();
				} );
			}
			else {
				Helper.GetResponse( m[ 0 ], m[ 1 ], "releases", ( content ) => {
					var content2 = "{\"Items\":" + content + "}";
					var js = JsonHelper.FromJson<ReleaseJson>( content2 );
					m_js.AddRange( js );
					Repaint();
				} );
			}
		}


		void GetTagsResponse( string gitURL ) {
			var m = Helper.ParseURL( gitURL );
			Helper.GetResponse( m[ 0 ], m[ 1 ], "tags" );
		}

		string[] GetVersions() {
			var ss = m_js.Select( x => x.tag_name ).ToArray();
			return ss;
		}



		public (Texture2D, string, bool) GetInstallPackageInfo( string packageName ) {
			var (find, revisionName, revisionLock) = Helper.GetInstallPackageInfo( packageName );
			if( find ) {
				return (s_styles.IconIndicatorON, revisionName, revisionLock);
			}
			return (s_styles.IconIndicatorOFF, revisionName, revisionLock);
		}



		public void GUIInstallButton( E.GitURL gitURL ) {
			var (rev, hash) = Helper.GetLockData( gitURL.packageName );
			//if( rev == string.Empty ) return;

			Tags[] tags = null;
			var read = Helper.ReadWebResponseToFile(
				Helper.ParseURL( gitURL.url ),
				"tags",
				( content ) => tags = JsonHelper.FromJson<Tags>( "{\"Items\":" + content + "}" ) );

			if( !read ) {
				EditorGUILayout.HelpBox( "\"tags.json\" Not Found. Please Refresh Button Push.", MessageType.Warning );
				return;
			}

			var (ico, revisionName, revisionLock) = GetInstallPackageInfo( gitURL.packageName );
			var buttonName = $"{gitURL.packageName}";
			var rName = revisionName;
			if( rName != string.Empty ) {
				if( rName == "HEAD" ) {
					var t = tags.GetTags( hash );
					if( t != null ) {
						rName = t.name;
					}
				}
				buttonName += $" - {rName} [{hash}]";
			}
			if( GUILayout.Button( new GUIContent( buttonName, ico ), s_styles.ExposablePopupItem, GUILayout.ExpandWidth( false ) ) ) {
				if( revisionName != string.Empty ) {

					var m = new GenericMenu();
					//if( revisionName == "HEAD" ) {
						m.AddItem( new GUIContent( $"Update [{revisionName}]" ), false, () => {
							GetTagsResponse( gitURL.url );
							Helper.UpdatePackageHEAD( gitURL.packageName );
						} );
					//}
					m.AddItem( new GUIContent( "Uninstall Package" ), false, () => Helper.UninstallPackage( gitURL.packageName ) );
					if( !revisionLock ) {
						m.AddSeparator( "" );

						//m.AddItem( new GUIContent( "HEAD" ), rev == "HEAD", () => { } );
						var vers = GetVersions();
						foreach( var v in tags ) {
							m.AddItem( new GUIContent( $"{v.name}: {v.commit.sha}" ), hash == v.commit.sha, ( _rev ) => {
								Helper.ChangeRevision( gitURL.packageName, (string) _rev, tags.GetRevisionHash((string) _rev ) );
								}, v.name );
						}
					}
					m.DropDown( new Rect( Event.current.mousePosition, new Vector2( 0, 0 ) ) );
				}
				else {
					var m = new GenericMenu();
					m.AddItem( new GUIContent( "Install Package" ), false, () => {
						Task.Run( () => InstallPackageProcess( gitURL ) );
					} );
					m.DropDown( new Rect( Event.current.mousePosition, new Vector2( 0, 0 ) ) );
				}
			}
		}



		public Texture2D GetDownloadIndicatorIcon( string url, string name, string repoName, string tag, string extention = "" ) {
			if( Helper.IsDownloaded( url, name, repoName, tag, extention ) ) {
				return s_styles.IconIndicatorON;
			}
			return s_styles.IconIndicatorOFF;
		}



		public void GUIDownloadButton( string url, string name, string repoName, string tag, string extention = "" ) {
			GUIDownloadButton( GetFileName( url ), url, name, repoName, tag, extention );
		}


		public void GUIDownloadButton( string title, string url, string name, string repoName, string tag, string extention = "" ) {
			var ico = GetDownloadIndicatorIcon( url, name, repoName, tag, extention );
			if( GUILayout.Button( new GUIContent( title, ico ), s_styles.ExposablePopupItem, GUILayout.ExpandWidth( false ) ) ) {
				if( Helper.IsDownloaded( url, name, repoName, tag, extention ) ) {
					var m = new GenericMenu();
					m.AddItem( new GUIContent( "Re-download" ), false, () => {
						DownloadFile( url, name, repoName, tag, extention );
					} );
					m.AddItem( new GUIContent( "Run in shell" ), false, () => {
						var p = new System.Diagnostics.Process();
						p.StartInfo.FileName = Helper.GetDownloadFileName( url, name, repoName, tag, extention );
						p.Start();
					} );

					m.DropDown( new Rect( Event.current.mousePosition, new Vector2( 0, 0 ) ) );
				}
				else {
					var m = new GenericMenu();
					m.AddItem( new GUIContent( "Download" ), false, () => {
						DownloadFile( url, name, repoName, tag, extention );
					} );
					m.DropDown( new Rect( Event.current.mousePosition, new Vector2( 0, 0 ) ) );
				}
			}
		}


		public void DownloadFile( string url, string name, string repoName, string tag, string extention = "" ) {
			if( RequestStatus.networking ) {
				EditorUtility.DisplayDialog( "Warning", "No new downloads can be added during download", "OK" );
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

				using( WebClient wc = new WebClient() ) {
					RequestStatus.Begin( $"Download File {GetFileName( url )}" );
					wc.Headers.Add( "User-Agent", "Nothing" );
					wc.DownloadProgressChanged += ( sender, e ) => {
						RequestStatus.networkingMsg = $"Download File {GetFileName( url )} {e.ProgressPercentage} %";
					};
					wc.DownloadFileCompleted += ( sender, e ) => {
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


		void Refresh() {
			enableLatest = E.i.showMode != 0;
			MakeDownloadList();
			ReadWebResponseToFile( enableLatest );
			indexChanged = true;
			Helper.s_manifestJsonCache = null;
			RequestStatus.Reset();
		}


		void DrawToolbar() {
			GUILayout.BeginHorizontal( EditorStyles.toolbar );
			{
				if( GUILayout.Button( s_styles.IconSetting, EditorStyles.toolbarButton ) ) {
					EditorApplication.ExecuteMenuItem( "Edit/Preferences..." );
				}
				EditorGUI.BeginChangeCheck();
				bool force = false;

				if( 0 < E.i.gitUrls.Count ) {
					AdjustSelectURL();

					var a = E.i.gitUrls.Aggregate( "", ( max, cur ) => max.Length > cur.url.Length ? max : cur.url );

					var content = new GUIContent( GetFileNameWithoutExtension( a ) );
					var size = s_styles.toolbarDropDown.CalcSize( content );
					var rc = GUILayoutUtility.GetRect( content, s_styles.toolbarDropDown, GUILayout.Width( size.x + 16 ) );
					if( rc.Contains( Event.current.mousePosition ) && Event.current.type == EventType.MouseDown && Event.current.button == 0 ) {
						var m = new GenericMenu();
						for( int i = 0; i < E.i.gitUrls.Count; i++ ) {
							var p = E.i.gitUrls[ i ];
							m.AddItem( new GUIContent( Helper.ParseURLToPopup( p.url ) ), false, ( idx ) => {
								m_selectURL = (int) idx;
								Refresh();
							}, i );
						}
						m.DropDown( new Rect( rc.x, rc.y + 6, rc.width, 12 ) );
						Event.current.Use();
					}
					GUI.Button( rc, GetFileNameWithoutExtension( E.i.gitUrls[ m_selectURL ].url ), s_styles.toolbarDropDown );
				}

				E.i.showMode = EditorGUILayout.Popup( E.i.showMode, showRelease, s_styles.toolbarDropDown, GUILayout.Width( 120 ) );

				if( EditorGUI.EndChangeCheck() || force ) {
					Refresh();
				}

				if( GUILayout.Button( s_styles.IconRefresh, EditorStyles.toolbarButton ) ) {
					GetReleasesResponse( GetCurrentURL(), enableLatest );
				}
				GUILayout.FlexibleSpace();
				if( GUILayout.Button( "manifest.json", EditorStyles.toolbarButton ) ) {
					var p = new System.Diagnostics.Process();
					p.StartInfo.FileName = $"{Environment.CurrentDirectory.Replace("\\","/")}/Packages/manifest.json";
					p.Start();
				}
				if( GUILayout.Button( "Package Manager", EditorStyles.toolbarButton ) ) {
					EditorApplication.ExecuteMenuItem( "Window/Package Manager" );
				}
			}
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

			var info = Helper.ParseURL( GetCurrentURL() );

			if( info == null ) {
				EditorGUILayout.HelpBox( "Set URL from preferences", MessageType.Info );
				return;
			}
			if( RequestStatus.networkError ) {
				EditorGUILayout.HelpBox( RequestStatus.networkingErrorMsg, MessageType.Error );
				return;
			}

			using( new GUILayout.HorizontalScope() ) {
				bool force = false;
				if( indexChanged ) {
					indexChanged = false;
					if( m_js == null || m_js.Count == 0 ) {
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
						var data = E.GetData( GetCurrentURL() );
						if( data.enablePackage ) {
							using( new GUILayout.HorizontalScope( s_styles.helpBox ) ) {
								GUIInstallButton( data );
							}
							GUILayout.Space( 4 );
						}

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
													s_packagePath = fname;
													s_interactive = true;
													EditorApplication.update += DeleyImportPackage;
												}
											}
										}
									}
								}

								if( !p.toggle ) continue;

								EditorGUILayout.LabelField( p.body, s_styles.helpBox );

								foreach( var asset in p.assets ) {
									GUIDownloadButton( asset.browser_download_url, info[ 0 ], info[ 1 ], p.tag_name );
								}

								if( !string.IsNullOrEmpty( p.zipball_url ) ) {
									GUIDownloadButton( "Source code (zip)", p.zipball_url, info[ 0 ], info[ 1 ], p.tag_name, ".zip" );
								}
								if( !string.IsNullOrEmpty( p.tarball_url ) ) {
									GUIDownloadButton( "Source code (tar.gz)", p.tarball_url, info[ 0 ], info[ 1 ], p.tag_name, ".tar.gz" );
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
				Debug.LogException( e );
			}
		}



		string s_packagePath;
		bool s_interactive;
		void DeleyImportPackage() {
			EditorApplication.update -= DeleyImportPackage;
			AssetDatabase.ImportPackage( s_packagePath, s_interactive );
		}


		


		List<(string, string)> m_dependenciesPackages;

		void InstallPackageProcess( E.GitURL gitURL ) {
			m_dependenciesPackages = new List<(string, string)>();

			var uu = gitURL.url.Split( '/' );
			var revision = gitURL.branchName;
			if( string.IsNullOrEmpty( revision ) ) {
				revision = "HEAD";
			}

			if( string.IsNullOrEmpty( gitURL.branchName ) ) {
				m_dependenciesPackages.Add( (gitURL.packageName, $"{GetCurrentURL()}") );
			}
			else {
				m_dependenciesPackages.Add( (gitURL.packageName, $"{GetCurrentURL()}#{gitURL.branchName}") );
			}

			string url = $"https://raw.githubusercontent.com/{uu[ 3 ]}/{GetFileNameWithoutExtension( uu[ 4 ] )}/{revision}/package.json";
			//Debug.Log( url );

			if( !DependencyResolution( uu[ 3 ], GetFileNameWithoutExtension( uu[ 4 ] ), revision ) ) {
				Debug.LogError( "Error: InstallPackageProcess" );
				return;
			}

			//var manifest_json = ManifestJson.Deserialize( File.ReadAllText( "Packages/manifest.json" ) ) as Dictionary<string, object>;
			var dic = (System.Collections.IDictionary) Helper._manifestJsonCache[ "dependencies" ];

			foreach( var p in m_dependenciesPackages ) {
				if( !dic.Contains( p.Item1 ) ) {
					//Debug.Log( $"{p.Item1} ; {p.Item2}" );
					dic.Add( p.Item1, p.Item2 );
				}
			}

			Helper.RefreshManifestJson();

			Debug.Log( "Complete: InstallPackageProcess" );
		}


		bool DependencyResolution( string name, string packageName, string revision ) {

			List<(string, string)> dependenciesPackages = new List<(string, string)>();

			using( var wc = new WebClient() ) {
				try {
					string url = $"https://raw.githubusercontent.com/{name}/{packageName}/{revision}/package.json";
					var packageJson = wc.DownloadString( new Uri( url ) );

					var obj = ManifestJson.Deserialize( packageJson );
					Dictionary<string, object> dictionary = obj as Dictionary<string, object>;
					try {
						var dic = (System.Collections.IDictionary) dictionary[ "dependencies" ];

						foreach( object current in dic.Keys ) {
							var pName = current as string;
							if( 0 <= m_dependenciesPackages.FindIndex( x => x.Item1 == pName ) ) continue;

							dependenciesPackages.Add( (pName, dic[ pName ] as string) );
						}
					}
					catch( KeyNotFoundException ) {
						// dependenciesが無い場合は無視
					}
				}
				catch( Exception exx ) {
					Debug.LogException( exx );
					return false;
				}
			}

			m_dependenciesPackages.AddRange( dependenciesPackages );

			foreach( var p in dependenciesPackages ) {
				var u = p.Item2.Split( '#' );
				var uu = Helper.ParseURL( u[ 0 ] );
				var rev = u.Length == 1 ? "" : u[ 1 ];
				if( string.IsNullOrEmpty( rev ) ) {
					rev = "HEAD";
				}
				if( !DependencyResolution( uu[ 0 ], uu[ 1 ], rev ) ) {
					return false;
				}
			}
			return true;
		}
	}
}

