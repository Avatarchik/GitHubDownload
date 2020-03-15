
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

using static System.IO.Path;

namespace Hananoki.GitHubDownload {
	[Serializable]
	public class GitHubDownloadSettingsEditor {

		public List<string> urls;

		[System.Serializable]
		public class GitURL {
			public string url;
			public bool enablePackage;
			public string branchName;
			public string packageName;
			//public string version;
			public GitURL( string url ) {
				this.url = url;
				enablePackage = false;
				branchName = "";
				packageName = "";
				//version = "";
			}
		}
		public List<GitURL> gitUrls;

		public string adb_exe;
		public string opendir;

		public int showMode;

		public static GitHubDownloadSettingsEditor i;

		public static string unityPreferencesFolder;

		public static string gitHubCacheDirectory {
			get {
#if UNITY_EDITOR_WIN
				return GetDirectoryName( ( GetDirectoryName( unityPreferencesFolder ) ) ).Replace( '\\', '/' ) + "/GitHub";
#elif UNITY_EDITOR_OSX
			return unityPreferencesFolder + "/" + "../../../Unity/GitHub";
#else
			return "";
#endif
			}
		}


		public GitHubDownloadSettingsEditor() {

		}

		public static void AddURLs( params string[] urls ) {
			//i.urls.AddRange( urls );
			//i.gitUrls = i.gitUrls.Distinct().ToList();
			foreach( var p in urls ) i.gitUrls.Add( new GitURL( p ) );
			i.gitUrls = i.gitUrls.Distinct( x => x.url ).ToList();
			Save();
		}

		public static GitURL GetData( string url ) {
			return i.gitUrls.Find( x => x.url == url );
		}

		public static void Load() {
			unityPreferencesFolder = InternalEditorUtility.unityPreferencesFolder;
			if( i != null ) return;

			i = Get( PackageInfo.editorPrefName );
			if( i == null ) {
				i = new GitHubDownloadSettingsEditor();
				Save();
			}

			if( i.urls != null ) {
				if( 1 <= i.urls.Count ) {
					i.gitUrls = new List<GitURL>();
					foreach( var p in i.urls ) i.gitUrls.Add( new GitURL( p ) );
					i.urls = null;
					Debug.Log( "Convert GitURL" );
					Save();
				}
			}

			i.gitUrls.Sort( ( x, y ) => string.Compare( x.url, y.url ) );
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
			string json = JsonUtility.ToJson( data , true);
			EditorPrefs.SetString( name, json );
		}
	}


	public static class IEnumerableExtensions {
		private sealed class CommonSelector<T, TKey> : IEqualityComparer<T> {
			private Func<T, TKey> m_selector;

			public CommonSelector( Func<T, TKey> selector ) {
				m_selector = selector;
			}

			public bool Equals( T x, T y ) {
				return m_selector( x ).Equals( m_selector( y ) );
			}

			public int GetHashCode( T obj ) {
				return m_selector( obj ).GetHashCode();
			}
		}

		public static IEnumerable<T> Distinct<T, TKey>(
				this IEnumerable<T> source,
				Func<T, TKey> selector
		) {
			return source.Distinct( new CommonSelector<T, TKey>( selector ) );
		}
	}

}
