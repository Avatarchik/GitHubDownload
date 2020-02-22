
using System;
using UnityEditor;
using UnityEngine;

namespace Hananoki.GitHubDownload {

	// @todo 多重に通信許可するならstaticは問題
	public static class RequestStatus {
		public static bool networking;

		public static string networkingMsg;

		public static bool networkError;
		public static string networkingErrorMsg;

		public static void Reset() {
			networkError = false;
		}

		public static void SetError(Exception e) {
			networkError = true;
			networkingErrorMsg = e.Message;
		}

		static float curTime;
		static float lastTime;
		public static int m_count;
		static float m_watiTime;

		
		public static void updateThreadSync() {
			curTime = Time.realtimeSinceStartup;
			float deltaTime = (float) ( curTime - lastTime );
			lastTime = curTime;

			m_watiTime -= deltaTime;
			if( m_watiTime < 0 ) {
				m_watiTime = 0.1250f;

				m_count++;
				if( 12 <= m_count ) {
					m_count = 0;
				}
				GitHubDownloadWindow.Repaint();
			}
		}
	}


	public class RequestStatusScope : IDisposable {
		public RequestStatusScope( string msg ) {
			RequestStatus.networking = true;
			RequestStatus.networkError = false;
			RequestStatus.networkingMsg = msg;
			EditorApplication.update += RequestStatus.updateThreadSync;
		}
		public void Dispose() {
			RequestStatus.networking = false;
			EditorApplication.update -= RequestStatus.updateThreadSync;
		}
	}
}
