
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
	}
}
