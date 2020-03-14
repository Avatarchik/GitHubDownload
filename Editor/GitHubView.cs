using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace Hananoki.GitHubDownload {
	public class GitHubView : EditorWindow {
		static BindingFlags fullBinding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
		ScriptableObject webview;
		static MethodInfo setSizeAndPosition;

		static MethodInfo methodInfo_Back;
		static MethodInfo methodInfo_Forward;
		static MethodInfo methodInfo_Reload;

		public static void Open( string url ) {
			var window = GetWindow<GitHubView>();
			window.wantsMouseMove = true;

			window.titleContent = new GUIContent( "GitHubView", EditorGUIUtility.FindTexture( "UnityEditor.InspectorWindow" ) );

			if( window.webview == null ) {

				var dockArea = typeof( EditorWindow ).GetField( "m_Parent", fullBinding ).GetValue( window );

				//var webViewType = Types.GetType( "UnityEditor.WebView", "UnityEditor.dll" );
				var webViewType = Assembly.Load( "UnityEditor.dll" ).GetType( "UnityEditor.WebView" );
				var initWebView = webViewType.GetMethod( "InitWebView", fullBinding );
				var loadURL = webViewType.GetMethod( "LoadURL", fullBinding );
				setSizeAndPosition = webViewType.GetMethod( "SetSizeAndPosition", fullBinding );

				methodInfo_Back = webViewType.GetMethod( "Back", fullBinding );
				methodInfo_Forward = webViewType.GetMethod( "Forward", fullBinding );
				methodInfo_Reload = webViewType.GetMethod( "Reload", fullBinding );

				window.webview = CreateInstance( webViewType );


				initWebView.Invoke( window.webview, new object[] {
								dockArea,
								23,
								23,
								(int)300,
								(int)300,
								true
						} );

				loadURL.Invoke( window.webview, new object[] { url } );

			}
		}


		void OnGUI() {
			if( setSizeAndPosition != null ) {
				setSizeAndPosition.Invoke( webview, new object[] { 0, 20, (int) position.width, (int) position.height } );
			}

			//using( new GUILayout.HorizontalScope( EditorStyles.toolbarButton ) ) {
			//	if( GUILayout.Button( "戻る", EditorStyles.toolbarButton ) ) {
			//		methodInfo_Back.Invoke( webview, null );
			//	}
			//	if( GUILayout.Button( "進む", EditorStyles.toolbarButton ) ) {
			//		methodInfo_Forward.Invoke( webview, null );
			//	}
			//	if( GUILayout.Button( "リロード", EditorStyles.toolbarButton ) ) {
			//		methodInfo_Reload.Invoke( webview, null );
			//	}
			//	GUILayout.FlexibleSpace();
			//}
		}
	}
}
