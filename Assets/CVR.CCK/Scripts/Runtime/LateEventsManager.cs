using System;
using UnityEngine;

namespace CVR.CCK.Runtime {
	[DefaultExecutionOrder(int.MaxValue)]
	public class LateEventsManager : MonoBehaviour {
		public static Action OnPostFixedUpdate;
		public static Action OnPostUpdate;
		public static Action OnPostLateUpdate;

		private void Update() => OnPostUpdate?.Invoke();
		private void LateUpdate() => OnPostLateUpdate?.Invoke();
		private void FixedUpdate() => OnPostFixedUpdate?.Invoke();
		
		#if !CVR_CLIENT
		[RuntimeInitializeOnLoadMethod]
		public static void InitializeOnEnterPlayMode() => DontDestroyOnLoad(new GameObject("LateEventsManager", typeof(LateEventsManager)));
		#endif
	}
}