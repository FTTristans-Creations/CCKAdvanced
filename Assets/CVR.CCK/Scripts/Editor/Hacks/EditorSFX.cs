using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace CVR.CCKEditor.Hacks
{
    /// <summary>
    /// https://discussions.unity.com/t/way-to-play-audio-in-editor-using-an-editor-script/473638/13
    /// </summary>
    public static class EditorSfx
    {
        public static void PlayClip(AudioClip clip, int startSample = 0, bool loop = false)
        {
            try
            {
                Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
      
                Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
                MethodInfo method = audioUtilClass.GetMethod(
                    "PlayPreviewClip",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[] { typeof(AudioClip), typeof(int), typeof(bool) },
                    null
                );

                //Debug.Log(method); 
                method!.Invoke(
                    null,
                    new object[] { clip, startSample, loop }
                );
            }
            catch (Exception e)
            {
                Debug.LogWarning("Error attempting to play audio clip in editor: " + e.Message);
            }
        }

        public static void StopAllClips()
        {
            try
            {
                Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;

                Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
                MethodInfo method = audioUtilClass.GetMethod(
                    "StopAllPreviewClips",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new Type[] { },
                    null
                );
                
                //Debug.Log(method);
                method!.Invoke(
                    null,
                    new object[] { }
                );
            }
            catch (Exception e)
            {
                Debug.LogWarning("Error attempting to stop all audio clips in editor: " + e.Message);
            }
        }
    }
}