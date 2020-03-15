
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using System.Reflection;

using static System.IO.Path;

using E = Hananoki.GitHubDownload.GitHubDownloadSettingsEditor;

namespace Hananoki.GitHubDownload {
	public class GitURLConfig : EditorWindow {

		public E.GitURL gitURL;

		public static void Open( E.GitURL gitURL ) {
			var window = GetWindow<GitURLConfig>( true );
			window.titleContent = new GUIContent( "GitURLConfig" );
			window.gitURL = gitURL;
		}
		void OnLostFocus() {
			Close();
		}

		bool networking;
		string networkErr;

		void DrawGUI() {
			using( new GUILayout.VerticalScope( EditorStyles.helpBox ) ) {
				EditorGUI.BeginChangeCheck();
				gitURL.branchName = EditorGUILayout.TextField( "revision", gitURL.branchName );
				gitURL.enablePackage = EditorGUILayout.Toggle( "package.json", gitURL.enablePackage );
				gitURL.packageName = EditorGUILayout.TextField( "packageName", gitURL.packageName );
				//using( new EditorGUI.DisabledGroupScope( true ) ) {
				//	gitURL.version = EditorGUILayout.TextField( "version", gitURL.version );
				//}
				if( EditorGUI.EndChangeCheck() ) {
					E.Save();
				}
			}
			using( new GUILayout.HorizontalScope() ) {
				if( !string.IsNullOrEmpty( networkErr ) ) {
					GUILayout.Label( networkErr );
				}
				GUILayout.FlexibleSpace();
				//if( gitURL.packageName == string.Empty || gitURL.enablePackage ==false) {
				if( GUILayout.Button( "Get from GitHub", GUILayout.ExpandWidth( false ) ) ) {
					var wc = new WebClient();
					wc.DownloadStringCompleted += ( sender, e ) => {
						//networking = false;
						if( e.Error == null ) {
							var obj = ManifestJson.Deserialize( e.Result );
							Dictionary<string, object> dictionary = obj as Dictionary<string, object>;
							gitURL.packageName = dictionary[ "name" ] as string;
							//gitURL.version = dictionary[ "version" ] as string;
							gitURL.enablePackage = true;
							E.Save();
							Repaint();
						}
						else {
							networkErr = "package.json: (404) Not Found.";
							gitURL.packageName = "";
							gitURL.enablePackage = false;
							Repaint();
						}
					};
					var uu = gitURL.url.Split( '/' );
					Debug.Log( uu[ 3 ] );
					Debug.Log( uu[ 4 ] );
					var branch = gitURL.branchName;
					if( string.IsNullOrEmpty( branch ) ) {
						branch = "HEAD";
					}
					wc.DownloadStringAsync( new Uri( $"https://raw.githubusercontent.com/{uu[ 3 ]}/{GetFileNameWithoutExtension( uu[ 4 ] )}/{branch}/package.json" ) );
					networkErr = "";
				}
				//}
			}
		}


		void OnGUI() {
			try {
				DrawGUI();
			}
			catch( Exception e ) {
				Debug.LogException( e );
			}
		}
	}
}

