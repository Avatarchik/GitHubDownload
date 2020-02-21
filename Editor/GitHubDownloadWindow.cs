using System;
using System.IO;

using System.Net;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;

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
		public string m_repoName;

		public class Styles {
			public GUIStyle ExposablePopupItem;
			public GUIStyle boldLabel;
			public GUIStyle miniBoldLabel;
			public GUIStyle helpBox;
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
			}
		}

		public static Styles s_styles;


		[MenuItem( "Window/GitHubDownload" )]
		public static void Open() {
			var window = GetWindow<GitHubDownloadWindow>();
			window.titleContent = new GUIContent( "GitHubDownload" );
		}


		void OnEnable() {
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





		void DrawGUI() {
			if( s_styles == null ) s_styles = new Styles();

			using( new GUILayout.HorizontalScope() ) {
				m_selectURL = EditorGUILayout.Popup( m_selectURL, urls.Select( x => GetFileNameWithoutExtension( x ) ).ToArray() );
				if( GUILayout.Button( "Get Releases Latest" ) ) {
					m_js = null;
					m_js = GetReleasesLatest( urls[ m_selectURL ] );
					m_repoName = ParseURL( urls[ m_selectURL ] )[ 1 ];
				}
			}

			if( m_js != null ) {
				GUILayout.Label( m_repoName, s_styles.boldLabel );
				var rc = GUILayoutUtility.GetLastRect();
				rc.x += s_styles.boldLabel.CalcSize( new GUIContent( m_repoName ) ).x;
				rc.x += 8;
				GUI.Label( rc, m_js.tag_name, s_styles.miniBoldLabel );

				EditorGUILayout.LabelField( m_js.body, s_styles.helpBox );

				if( GUILayout.Button( new GUIContent( GetFileName( m_js.assets[ 0 ].browser_download_url ), EditorGUIUtility.FindTexture( "winbtn_mac_inact" ) ), s_styles.ExposablePopupItem, GUILayout.ExpandWidth( false ) ) ) {
					using( WebClient wc = new WebClient() ) {
						var fname = GetFileName( m_js.assets[ 0 ].browser_download_url );
						fname = $"{Environment.CurrentDirectory}/{fname}";

						wc.DownloadFile( new Uri( m_js.assets[ 0 ].browser_download_url ), fname );
					}
				}

				if( GUILayout.Button( new GUIContent( "Source code (zip)", EditorGUIUtility.FindTexture( "winbtn_mac_inact" ) ), s_styles.ExposablePopupItem, GUILayout.ExpandWidth( false ) ) ) {
				}
				if( GUILayout.Button( new GUIContent( "Source code (tar.gz)", EditorGUIUtility.FindTexture( "winbtn_mac_inact" ) ), s_styles.ExposablePopupItem, GUILayout.ExpandWidth( false ) ) ) {
				}
			}

		}


		/// <summary>
		/// 
		/// </summary>
		void OnGUI() {
			DrawGUI();
		}
	}
}
