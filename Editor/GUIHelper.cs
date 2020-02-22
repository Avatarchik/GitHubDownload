using UnityEditor;
using UnityEngine;

namespace Hananoki.GitHubDownload {
	public static class GUIHelper {
		public static class Styles {
			static GUIStyle s_IconButton;
			public static GUIStyle iconButton {
				get {
					if( s_IconButton == null ) {
#if UNITY_2019_3_OR_NEWER
						s_IconButton = new GUIStyle( "IconButton" );
#else
					s_IconButton = new GUIStyle( "IconButton" );
					s_IconButton.fixedWidth = s_IconButton.fixedHeight = 16;
					s_IconButton.padding = new RectOffset( 0, 0, 0, 0 );
					s_IconButton.margin = new RectOffset( 0, 0, 0, 0 );
					s_IconButton.stretchWidth = false;
					s_IconButton.stretchHeight = false;
#endif
					}
					return s_IconButton;
				}
			}

			static GUIStyle s_FoldoutText;
			public static GUIStyle foldoutText {
				get {
					if( s_FoldoutText == null ) {
						s_FoldoutText = new GUIStyle( "ExposablePopupItem" );
						s_FoldoutText.font = EditorStyles.boldLabel.font;
						s_FoldoutText.fontStyle = FontStyle.Bold;
						s_FoldoutText.margin = new RectOffset( 0, 0, 0, 0 );
					}
					return s_FoldoutText;
				}
			}


		}
		public static bool Foldout( bool foldout, string text ) {
			string ssss = "     " + text;

			var cont = new GUIContent( ssss );
			var sz = Styles.foldoutText.CalcSize( cont );
			var rc = GUILayoutUtility.GetRect( cont, Styles.foldoutText, GUILayout.Width( sz.x ) );
			//EditorGUI.DrawRect( GUILayoutUtility.GetLastRect(),new Color(0,0,1,0.2f));
			var rc2 = rc;
			rc2.x += 4;
			foldout = EditorGUI.Foldout( rc2, foldout, "" );
			GUI.Button( rc, cont, Styles.foldoutText );
			return foldout;
		}
		public static bool IconButton( Rect position, Texture2D tex, float heighOffset = 0 ) {
			return IconButton( position, tex, Styles.iconButton, heighOffset );
		}
		public static bool IconButton( Rect position, Texture2D tex, GUIStyle style, float heighOffset = 0 ) {
			bool result = false;
			position.y += heighOffset;
			if( HasMouseClick( position ) ) {
				Event.current.Use();
				result = true;
			}
			GUI.Button( position, new GUIContent( tex ), style );
			return result;
		}
		public static bool IconButton( Texture2D tex, int marginHeighOffset = 0 ) {
			var style = new GUIStyle( Styles.iconButton );
			style.margin = new RectOffset( 0, 0, marginHeighOffset, 0 );
			var r = GetLayout( tex, style );
			return IconButton( r, tex, style, 0 );
		}
		public static Rect GetLayout( Texture2D tex, GUIStyle style, params GUILayoutOption[] option ) {
			return GUILayoutUtility.GetRect( new GUIContent( tex ), style, option );
		}
		public static bool HasMouseClick( Rect rc, int type = 0 ) {
			var ev = Event.current;
			var pos = ev.mousePosition;
			if( ev.type == EventType.MouseDown && ev.button == (int) type ) {
				if( rc.x < pos.x && pos.x < rc.max.x && rc.y < pos.y && pos.y < rc.max.y ) {
					return true;
				}
			}
			return false;
		}
	}
	
}