using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace ABI.CCK.Scripts.Editor
{
    /// <summary>
    /// Reflection compatibility stub. Third-party editor tools locate the CCK build
    /// utility via Assembly.Load("Assembly-CSharp-Editor").GetType(
    /// "ABI.CCK.Scripts.Editor.CCK_BuildUtility"), which stopped working when the CCK
    /// moved into the CVR.CCK asmdef. This type lives in Assembly-CSharp-Editor (it
    /// has no asmdef coverage) so those lookups resolve again. The fields forward
    /// the REAL event instances from the actual CCK_BuildUtility inside CVR.CCK,
    /// so listeners subscribe to the genuine build pipeline.
    /// </summary>
    internal static class CCK_BuildUtility
    {
        public static readonly UnityEvent<GameObject> PreAvatarBundleEvent = Resolve("PreAvatarBundleEvent");
        public static readonly UnityEvent<GameObject> PrePropBundleEvent = Resolve("PrePropBundleEvent");

        private static UnityEvent<GameObject> Resolve(string fieldName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type;
                try
                {
                    type = assembly.GetType("ABI.CCK.Scripts.Editor.CCK_BuildUtility", false);
                }
                catch
                {
                    continue;
                }

                // Skip ourselves: the stub has the same full name in this assembly.
                if (type == null || type.Assembly == typeof(CCK_BuildUtility).Assembly)
                {
                    continue;
                }

                var field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.Public);
                if (field?.GetValue(null) is UnityEvent<GameObject> unityEvent)
                {
                    return unityEvent;
                }
            }

            // CCK not installed: return a dead event so subscribers don't NRE.
            return new UnityEvent<GameObject>();
        }
    }
}
