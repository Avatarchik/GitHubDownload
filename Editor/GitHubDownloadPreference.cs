
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

using E = Hananoki.GitHubDownload.GitHubDownloadSettingsEditor;

namespace Hananoki.GitHubDownload {

	public class GitHubDownloadPreference {
		public class Styles {
			public GUIStyle IconButton;
			public Texture2D ol_plus;
			public Texture2D ol_minus;
			public Texture2D Favorite;
			public Styles() {
				ol_plus =  AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath( "ad645bf147d15d64f9bfd8d9261df17b" ));
				ol_minus = AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( "ea88f29401a564148a8356c8a9141177" ) );
				Favorite = EditorGUIUtility.FindTexture( "Favorite" );
#if UNITY_2019_3_OR_NEWER
				IconButton = new GUIStyle( "IconButton" );
#else
				IconButton = new GUIStyle( "IconButton" );
				IconButton.fixedWidth = IconButton.fixedHeight = 16;
				IconButton.padding = new RectOffset( 0, 0, 0, 0 );
				IconButton.margin = new RectOffset( 0, 0, 0, 0 );
				IconButton.stretchWidth = false;
				IconButton.stretchHeight = false;
#endif
			}
		}

		public static Styles s_styles;
		static Vector2 m_scroll2;

		public static bool s_changed;
		static ReorderableList s_rl;



		static ReorderableList MakeRL() {
			var r = new ReorderableList( E.i.urls, typeof( string ) );

			r.drawHeaderCallback = ( rect ) => {
				EditorGUI.LabelField( rect, "SceneHierarchyWindow - SearchFilter" );
			};

			r.onAddCallback = ( rect ) => {
				if( E.i.urls.Count == 0 ) {
					E.i.urls.Add( "" );
				}
				else {
					E.i.urls.Add( E.i.urls[ r.count - 1 ] );
				}
			};

			r.drawElementCallback = ( rect, index, isActive, isFocused ) => {
				EditorGUI.BeginChangeCheck();
				var p = E.i.urls[ index ];
				var w = rect.width;
				var x = rect.x;
				rect.y += 1;
				//rect.height = EditorGUIUtility.singleLineHeight;
				//rect.width = w * 0.20f;
				//p.searchMode = (USceneHierarchyWindow.SearchMode) EditorGUI.EnumPopup( rect, p.searchMode, "MiniPopup" );

				//rect.x = x + w * 0.20f;
				//rect.width = w * 0.80f;
				E.i.urls[ index ] = EditorGUI.TextField( rect, p );

				//rect.x = x + w * 0.5f;
				//rect.width = w * 0.5f;

				//rect.x += 2;
				//p.exec = EditorGUI.TextField( rect, p.exec );
				//if( EditorGUI.EndChangeCheck() ) {
				//	s_changed = true;
				//}
				//rect.x += rect.width;
				//rect.width = 16;

			};
			//r.elementHeight = EditorGUIUtility.singleLineHeight;

			return r;
		}


		static void DrawCommandTable( ReorderableList r ) {
			EditorGUI.BeginChangeCheck();
			r.DoLayoutList();
			if( EditorGUI.EndChangeCheck() ) {
				s_changed = true;
			}
		}


		static string CheckURL( string url ) {
			string s = url.Trim();
			if( !s.StartsWith( "https://github.com" ) ) return string.Empty;

			return s;
		}


		static void OpenFilePanel( ) {
			var fname = EditorUtility.OpenFilePanel( "Please select a file", E.i.opendir, "" );
			if( !string.IsNullOrEmpty( fname ) ) {
				using( var st = new StreamReader( fname ) ) {
					var lst = new List<string>();
					var sss = st.ReadToEnd();
					if( !string.IsNullOrEmpty( sss ) ) {
						var ss = sss.Split( '\n' );
						for( int i = 0; i < ss.Length; i++ ) {
							var s = ss[ i ];
							s = s.TrimEnd( '\r' );
							if( !string.IsNullOrEmpty( s ) ) {
								lst.Add( s );
							}
						}
					}
					E.AddURLs( lst.ToArray() );
					GitHubDownloadWindow.Repaint();
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		static void DrawGUI() {
			E.Load();
			if( s_styles == null ) s_styles = new Styles();

			if( E.i.urls == null ) {
				E.i.urls = new List<string>();
				s_changed = true;
			}
			if( s_rl == null ) {
				s_rl = MakeRL();
			}

			EditorGUI.BeginChangeCheck();

			GUILayout.Label( "Enter URL" );
			using( new GUILayout.HorizontalScope() ) {
				EditorGUI.BeginChangeCheck();
				var _t = EditorGUILayout.TextField( E.i.adb_exe );
				if( EditorGUI.EndChangeCheck()) {
					E.i.adb_exe = _t;
				}
				var r = GUILayoutUtility.GetRect( new GUIContent( s_styles.ol_plus ), s_styles.IconButton );
				r.y += 3;
				if( GUI.Button( r, s_styles.ol_plus, s_styles.IconButton ) ) {
					var a = CheckURL( E.i.adb_exe );
					if( !string.IsNullOrEmpty( a ) ) {
						E.AddURLs( a );
						GitHubDownloadWindow.Repaint();
					}
				}
				r = GUILayoutUtility.GetRect( new GUIContent( s_styles.Favorite ), s_styles.IconButton );
				r.y += 3;
				if( GUI.Button( r, s_styles.Favorite, s_styles.IconButton ) ) {
					OpenFilePanel();
				}
			}

			GUILayout.Space(8);

			int i = 0;
			int delIndex = -1;
			foreach( var s in E.i.urls ) {
				using( new GUILayout.HorizontalScope( EditorStyles.helpBox ) ) {
					GUILayout.Label( s );
					if( GUILayout.Button( s_styles.ol_minus, s_styles.IconButton ) ) {
						delIndex = i;
					}
				}
				i++;
			}
			if( 0 <= delIndex ) {
				E.i.urls.RemoveAt( delIndex );
				s_changed = true;
			}
			//DrawCommandTable( s_rl );

			

			//using( var sc = new GUILayout.ScrollViewScope( m_scroll2 ) ) {
			//	m_scroll2 = sc.scrollPosition;

			//	var targetGroupList = PlatformUtils.GetSupportList();

			//	foreach( var t in targetGroupList ) {
			//		EditorGUI.BeginChangeCheck();
			//		var _b = HEditorGUILayout.ToggleLeft( P.GetPlatform( t ).enable, t.Icon(), t.GetName() );
			//		if( EditorGUI.EndChangeCheck() ) {
			//			P.GetPlatform( t ).enable = _b;
			//			BuildManagerWindow.ChangeActiveTarget();
			//		}
			//	}
			//}

			if( EditorGUI.EndChangeCheck() || s_changed ) {
				E.Save();
			}

			GUILayout.Space( 8f );

		}


#if UNITY_2018_3_OR_NEWER

		[SettingsProvider]
		public static SettingsProvider PreferenceView() {
			var provider = new SettingsProvider( $"Preferences/Hananoki/{PackageInfo.name}", SettingsScope.User ) {
				label = $"{PackageInfo.name}",
				guiHandler = PreferencesGUI,
				titleBarGuiHandler = () => GUILayout.Label( $"{PackageInfo.version}", EditorStyles.miniLabel ),
			};
			return provider;
		}
		public static void PreferencesGUI( string searchText ) {
#else
		[PreferenceItem( PackageInfo.name )]
		public static void PreferencesGUI() {
#endif
			using( new GUILayout.HorizontalScope() ) {
				GUILayout.Space( 8 );
				using( new GUILayout.VerticalScope() ) {
					DrawGUI();
				}
				GUILayout.Space( 8 );
			}

		}
	}
}
