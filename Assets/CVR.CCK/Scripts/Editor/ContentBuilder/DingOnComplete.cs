using CVR.CCKEditor.ContentBuilder;
using CVR.CCKEditor.Hacks;
using CVR.CCKEditor.Tools;
using UnityEditor;
using UnityEngine;

namespace CVR.CCKEditor.ContentBuilder
{
    public static class DingOnComplete
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            ContentBuilderAPI.OnStatusUpdated += OnBuildStatusUpdated;
        }

        private static void OnBuildStatusUpdated(ContentBuilderAPI.BuildStatus status)
        {
            if (status != ContentBuilderAPI.BuildStatus.Completed) return;
            if (!CCKEditorPrefs.DingOnComplete) return;
            PlayDing();
        }

        public static void PlayDing()
        {
            AudioClip ourClip = Resources.Load<AudioClip>("CCK/SFX/control-panel-upload-complete");
            if (!ourClip)
            {
                Debug.LogError("[CVR.CCK] The upload complete SFX clip was not found!");
                return;
            }
            EditorSfx.PlayClip(ourClip);
        }
    }
}