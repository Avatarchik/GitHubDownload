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

		
	}
}
