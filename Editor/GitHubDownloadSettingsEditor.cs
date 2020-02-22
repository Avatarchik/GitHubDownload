
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Hananoki.GitHubDownload {
	[Serializable]
	public class GitHubDownloadSettingsEditor {

		public List<string> urls;

		public string adb_exe;
		public string opendir;
		public int fold;

		
		public static GitHubDownloadSettingsEditor i;

		public GitHubDownloadSettingsEditor() {
			
		}

		public static void AddURLs( params string[] urls ) {
			i.urls.AddRange( urls );
			i.urls = i.urls.Distinct().ToList();
			Save();
		}


		public static void Load() {
			if( i != null ) return;
			i = Get( PackageInfo.editorPrefName );
			if( i == null ) {
				i = new GitHubDownloadSettingsEditor();
				Save();
			}
		}

		public static void Save() {
			Set( PackageInfo.editorPrefName, i );
		}


		public static GitHubDownloadSettingsEditor Get( string name ) {
			var lst = JsonUtility.FromJson<GitHubDownloadSettingsEditor>( EditorPrefs.GetString( name, "" ) );
			if( lst == null ) {
				return new GitHubDownloadSettingsEditor();
			}
			return lst;
		}

		public static void Set( string name, GitHubDownloadSettingsEditor data ) {
			string json = JsonUtility.ToJson( data );
#if LOCAL_DEBUG
			Debug.Log( json );
#endif
			EditorPrefs.SetString( name, json );
		}
	}
}
