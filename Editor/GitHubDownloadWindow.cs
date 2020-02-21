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

namespace Hananoki.GitHubDownload {

	public class GitHubDownloadWindow : EditorWindow {

		string githubURL = "https://api.github.com/repos";

		string[] urls = {
			"https://github.com/hananoki/HananokiSharedModule.git",
			"https://github.com/hananoki/HierarchyDropDown.git",
			};

		public int m_selectURL;
		public ReleaseJson m_js;

		public class Styles {
			public GUIStyle ExposablePopupItem;
			public GUIStyle boldLabel;
			public GUIStyle miniBoldLabel;
			public GUIStyle helpBox;

			public Texture2D Icon;
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
				Icon = EditorGUIUtility.FindTexture( "winbtn_mac_inact" );
			}
		}

		public static Styles s_styles;


		[MenuItem( "Window/GitHubDownload" )]
		public static void Open() {
			var window = GetWindow<GitHubDownloadWindow>();
			window.titleContent = new GUIContent( "GitHubDownload" );
		}


		void OnEnable() {
			MakeDownloadFIles();
		}


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

		ReleaseJson GetReleasesLatest( string name, string repoName ) {
			using( var client = new WebClient() ) {
				var url = $"{githubURL}/{name}/{repoName}/releases/latest";

				client.Headers.Add( "User-Agent", "Nothing" );

				var content = client.DownloadString( url );
				return JsonUtility.FromJson<ReleaseJson>( content );
			}
		}

		ReleaseJson GetReleasesLatest( string gitURL ) {
			var m = ParseURL( gitURL );
			return GetReleasesLatest( m[ 0 ], m[ 1 ] );
		}

		string[] ParseURL( string gitURL ) {
			var m = Regex.Matches( gitURL, @"^(https://github.com)/(.*)" );
			string[] ss = m[ 0 ].Groups[ 2 ].Value.Split( '/' );

			return new string[] { ss[ 0 ], GetFileNameWithoutExtension( ss[ 1 ] ) };
		}



		string[] m_downloadFiles;
		static Vector2 m_scroll;


		string GetCurrentURL() {
			return urls[ m_selectURL ];
		}


		void MakeDownloadFIles() {
			m_downloadFiles = new string[ 0 ];

			var info = ParseURL( GetCurrentURL() );
			var outputDirectory = $"{gitHubCacheDirectory}/{info[ 0 ]}/{info[ 1 ]}";

			if( !Directory.Exists( outputDirectory ) ) return;

			m_downloadFiles = Directory.GetDirectories( outputDirectory );
		}


		void SaveJson( ReleaseJson js ) {
			if( js == null ) return;
			var info = ParseURL( urls[ m_selectURL ] );
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
			var info = ParseURL( urls[ m_selectURL ] );
			var opath = $"{gitHubCacheDirectory}/{info[ 0 ]}/{info[ 1 ]}";

			if( !Directory.Exists( opath ) ) return;

			using( var st = new StreamReader( $"{opath}/releases_latest.json" ) ) {
				m_js = JsonUtility.FromJson<ReleaseJson>( st.ReadToEnd() );
			}
		}


		public async void DownloadRelease( string url, string name, string repoName, string tag ) {
			var outputDirectory = $"{gitHubCacheDirectory}/{name}/{repoName}/{tag}";

			if( !Directory.Exists( outputDirectory ) ) {
				Directory.CreateDirectory( outputDirectory );
			}

			await Task.Run( () => {
				var fname = GetFileName( url );
				//System.Threading.Thread.Sleep( 10000 );
				using( WebClient wc = new WebClient() ) {
					wc.DownloadFile( new Uri( url ), outputDirectory + "/" + fname );
				}
			} );


			MakeDownloadFIles();
		}


		void DrawGUI() {
			if( s_styles == null ) s_styles = new Styles();

			var info = ParseURL( urls[ m_selectURL ] );

			using( new GUILayout.HorizontalScope() ) {
				EditorGUI.BeginChangeCheck();
				m_selectURL = EditorGUILayout.Popup( m_selectURL, urls.Select( x => GetFileNameWithoutExtension( x ) ).ToArray() );
				if( EditorGUI.EndChangeCheck() ) {
					MakeDownloadFIles();
					ReadJson();
				}

				if( GUILayout.Button( "Get Releases Latest" ) ) {
					m_js = null;
					m_js = GetReleasesLatest( urls[ m_selectURL ] );
					SaveJson( m_js );
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
							DownloadRelease( asset.browser_download_url, info[ 0 ], info[ 1 ], m_js.tag_name );
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

