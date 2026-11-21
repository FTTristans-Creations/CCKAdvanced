using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CVR.CCK.Scripts.Editor.Hacks
{
internal static class AudioValidationUtils
    {
        private static readonly MethodInfo GetImporterFromClipMethodInfo;
        private static readonly MethodInfo GetMinMaxDataMethodInfo;

        static AudioValidationUtils()
        {
            Type audioUtilType = Type.GetType("UnityEditor.AudioUtil,UnityEditor");
            GetImporterFromClipMethodInfo = audioUtilType?.GetMethod(
                "GetImporterFromClip", BindingFlags.Static | BindingFlags.Public);
            GetMinMaxDataMethodInfo = audioUtilType?.GetMethod(
                "GetMinMaxData", BindingFlags.Static | BindingFlags.Public);
        }

        public static AudioImporter GetImporterFromClip(AudioClip clip)
        {
            if (GetImporterFromClipMethodInfo == null)
                return null;

            try
            {
                return GetImporterFromClipMethodInfo.Invoke(null, new object[] { clip }) as AudioImporter;
            }
            catch
            {
                return null;
            }
        }

        public static float[] TryGetMinMaxData(AudioClip clip)
        {
            if (GetImporterFromClipMethodInfo == null || GetMinMaxDataMethodInfo == null)
                return null;

            try
            {
                var importer = GetImporterFromClipMethodInfo.Invoke(null, new object[] { clip });
                if (importer == null)
                    return null;

                if (GetMinMaxDataMethodInfo.Invoke(null, new[] { importer }) is not float[] waveform
                    || waveform.Length == 0)
                    return null;

                return waveform;
            }
            catch
            {
                return null;
            }
        }

        public static List<AudioImporter> GetImporters(HashSet<Object> clips)
        {
            var importers = new List<AudioImporter>();
            foreach (Object obj in clips)
            {
                if (obj is AudioClip clip)
                {
                    AudioImporter importer = GetImporterFromClip(clip);
                    if (importer != null)
                        importers.Add(importer);
                }
            }
            return importers;
        }
    }

}