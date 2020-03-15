using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using UnityEditor;
using UnityEngine;
using System.IO.Compression;

using static System.IO.Path;

using E = Hananoki.GitHubDownload.GitHubDownloadSettingsEditor;

namespace Hananoki.GitHubDownload {
	public static class Helper {

		const string GITHUB_URL = "https://api.github.com/repos";

		


		public static string[] ParseURL( string gitURL ) {
			if( string.IsNullOrEmpty( gitURL ) ) return null;
			var m = Regex.Matches( gitURL, @"^(https://github.com)/(.*)" );
			string[] ss = m[ 0 ].Groups[ 2 ].Value.Split( '/' );

			return new string[] { ss[ 0 ], GetFileNameWithoutExtension( ss[ 1 ] ) };
		}

		public static string ParseURLToPopup( string gitURL ) {
			var ss = ParseURL( gitURL );
			return $"{ss[ 0 ]} : {ss[ 1 ]}";
		}


		public static string MakeOutputPath( string gitURL ) {
			var info = ParseURL( gitURL );
			return MakeOutputPath( info[ 0 ], info[ 1 ] );
		}

		public static string MakeOutputPath( string name, string repoName ) {
			return $"{E.gitHubCacheDirectory}/{name}/{repoName}";
		}

		


		public static string GetDownloadFileName( string url, string name, string repoName, string tag, string extention = "" ) {
			var outputDirectory = $"{E.gitHubCacheDirectory}/{name}/{repoName}/{tag}";
			string fname;
			if( string.IsNullOrEmpty( extention ) ) {
				fname = GetFileName( url );
			}
			else {
				fname = $"{repoName}-{tag}{extention}";
			}
			return outputDirectory + "/" + fname;
		}


		public static bool IsDownloaded( string url, string name, string repoName, string tag, string extention = "" ) {
			return File.Exists( GetDownloadFileName( url, name, repoName, tag, extention ) );
		}




		#region manifest.json

		public static Dictionary<string, object> s_manifestJsonCache = null;

		public static Dictionary<string, object> _manifestJsonCache {
			get {
				if( s_manifestJsonCache ==null) {
					s_manifestJsonCache = ManifestJson.Deserialize( File.ReadAllText( "Packages/manifest.json" ) ) as Dictionary<string, object>;
				}
				return s_manifestJsonCache;
			}
		}

		public static void RefreshManifestJson() {
			EditorApplication.update += _RefreshManifestJson;
		}

		static void _RefreshManifestJson() {
			EditorApplication.update -= _RefreshManifestJson;
			File.WriteAllText( "Packages/manifest.json", ManifestJson.Serialize( _manifestJsonCache, true ) );
			s_manifestJsonCache = null;
			AssetDatabase.Refresh();

		}


		public static (string, string) GetLockData( string packageName ) {
			if( !_manifestJsonCache .ContainsKey( "lock" ) ) return (string.Empty, string.Empty);
			var d1 = (IDictionary) _manifestJsonCache[ "lock" ];

			var d2 = d1[ packageName ] as Dictionary<string, object>;
			if( d2 == null ) return (string.Empty, string.Empty);
			
			return ( (string)d2[ "revision" ], (string) d2[ "hash" ]);
		}


		public static (bool, string, bool) GetInstallPackageInfo( string packageName ) {
			var dic = (IDictionary) _manifestJsonCache[ "dependencies" ];

			foreach( var p in dic.Keys ) {
				if( packageName.Contains( (string) p ) ) {
					var dic2 = (IDictionary) _manifestJsonCache[ "lock" ];
					foreach( var p2 in dic2.Keys ) {
						if( packageName.Contains( (string) p2 ) ) {
							var dic3 = dic2[ (string) p2 ] as Dictionary<string, object>;
							var url = (string) dic[ packageName ];
							var shp = url.Split( '#' );
							return (true, (string) dic3[ "revision" ], shp.Length == 2);
						}
					}
				}
			}
			return (false, "", false);
		}


		public static void ChangeRevision( string packageName, string newRevision, string newHash ) {
			var d1 = (IDictionary) _manifestJsonCache[ "lock" ];
			var d2 = d1[ packageName ] as Dictionary<string, object>;
			//d2[ "revision" ] = (object) newRevision;
			d2[ "hash" ] = (object) newHash;
			
			RefreshManifestJson();
		}

		public static void UpdatePackageHEAD( string packageName ) {
			var d1 = (IDictionary) _manifestJsonCache[ "lock" ];
			d1.Remove( packageName );
			RefreshManifestJson();
		}

		public static void UninstallPackage( string packageName ) {
			var d1 = (IDictionary) _manifestJsonCache[ "dependencies" ];
			d1.Remove( packageName );
			RefreshManifestJson();
		}


		#endregion



		public static bool ReadWebResponseToFile( string[] info, string getName, Action<string> responseAction = null ) {
			return ReadWebResponseToFile( info[ 0 ], info[ 1 ], getName, responseAction );
		}
		
		public static bool ReadWebResponseToFile( string name, string repoName, string getName, Action<string> responseAction = null ) {
			var opath = $"{E.gitHubCacheDirectory}/{name}/{repoName}";

			if( !Directory.Exists( opath ) ) return false;

			var ffname = getName.Replace( "/", "." );

			var fname = $"{opath}/{ffname}.json";
			if( !File.Exists( fname ) ) return false;

			using( var st = new StreamReader( fname ) ) {
				responseAction?.Invoke( st.ReadToEnd() );
			}
			return true;
		}



		public static void WriteWebResponseToFile( string name, string repoName, string content, string getName ) {
			if( string.IsNullOrEmpty( content ) ) return;
			var opath = MakeOutputPath( name, repoName );

			if( !Directory.Exists( opath ) ) {
				Directory.CreateDirectory( opath );
			}

			var fname = getName.Replace( "/", "." );

			opath = $"{opath}/{fname}.json";

			using( var st = new StreamWriter( opath ) ) {
				st.Write( content );
			}
		}



		public static void GetResponse( string name, string repoName, string getName, Action<string> responseAction = null ) {
			try {
				using( var wc = new WebClient() ) {
					var url = $"{GITHUB_URL}/{name}/{repoName}/{getName}";

					wc.Headers.Add( "User-Agent", "Nothing" );
					wc.DownloadStringCompleted += ( sender, e ) => {
						var content = e.Result;
						if( string.IsNullOrEmpty( content ) ) return;

						responseAction?.Invoke( content );
						WriteWebResponseToFile( name, repoName, content, getName );
						
					};
					wc.DownloadStringAsync( new Uri( url ) );

				}
			}
			catch( Exception e ) {
				Debug.LogException( e );
			}
		}
	}
}
