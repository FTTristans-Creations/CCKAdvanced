using System;
using UnityEngine;

namespace CVR.CCK.TestMode
{
    /// <summary>
    /// This behaviour runs as early as possible in playmode so we can run before all Awake and OnEnable calls.
    /// It will always exist in a users scene and hidden with hide flags.
    /// </summary>
    [AddComponentMenu("/")]
    [DefaultExecutionOrder(int.MinValue)]
    public class CCKTestModeActivator : MonoBehaviour
    {
        public static Action OnEarlyEnterPlayMode;
        private void Awake() => OnEarlyEnterPlayMode?.Invoke();
    }
}