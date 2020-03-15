
using System;

namespace Hananoki.GitHubDownload {
	[Serializable]
	public class ReleaseJson {
		[Serializable]
		public class Assets {
			public string browser_download_url;
		}
		public string url;
		public string tag_name;

		public Assets[] assets;
		public string tarball_url;
		public string zipball_url;
		public string body;

		[NonSerialized]
		public bool toggle;
	}



	[Serializable]
	public class Tags {
		public string name;
		public string tarball_url;
		public string zipball_url;
		[Serializable]
		public class Commit {
			public string sha;
			public string url;
		}
		public Commit commit;

		public string node_id;
	}

	public static class TagsExtention {
		public static Tags GetTags( this Tags[] tags, string hash ) {
			foreach( var p in tags ) {
				if( p.commit.sha == hash ) {
					return p;
				}
			}
			return null;
		}

		public static string GetRevisionHash( this Tags[] tags, string revision ) {
			foreach(var p in tags ) {
				if( p.name == revision ) {
					return p.commit.sha;
				}
			}
			return string.Empty;
		}
	}
}
