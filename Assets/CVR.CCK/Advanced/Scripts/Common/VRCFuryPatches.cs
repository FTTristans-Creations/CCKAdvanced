using System;
using System.Reflection;
using HarmonyLib;
using UnityEditor;
using UnityEngine;

namespace CVR.CCKEditor.Advanced
{
    /// <summary>
    /// Harmony patches applied to third-party packages (currently VRCFury) so the
    /// official, unmodified packages can be used alongside this CCK.
    /// Patches are resolved by name via reflection, so missing packages are ignored.
    /// </summary>
    [InitializeOnLoad]
    internal static class VRCFuryPatches
    {
        /// <summary>
        /// When true, VRCFury's parameter compressor is skipped for the duration of
        /// the current avatar preprocessing pass. Set by the VRChat-to-CVR converter,
        /// since VRChat's 256-bit parameter limit does not apply to CVR.
        /// </summary>
        public static bool SuppressParameterCompressor;

        private const string HarmonyId = "com.fttristan.cck-advanced";

        static VRCFuryPatches()
        {
            try
            {
                PatchParameterCompressor();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CCK Advanced] Failed to apply VRCFury patches: {e.Message}");
            }
        }

        private static void PatchParameterCompressor()
        {
            // VF.Service.Compressor.ParameterCompressorService.Apply() is the entry
            // point used by VRCFury's ParameterCompressorHook preprocessor.
            var serviceType = FindType("VF.Service.Compressor.ParameterCompressorService");
            var apply = serviceType?.GetMethod(
                "Apply",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (apply == null)
            {
                return; // VRCFury not installed or API changed - nothing to patch
            }

            var prefix = typeof(VRCFuryPatches).GetMethod(
                nameof(CompressorApplyPrefix),
                BindingFlags.Static | BindingFlags.NonPublic);

            new Harmony(HarmonyId).Patch(apply, prefix: new HarmonyMethod(prefix));
        }

        private static bool CompressorApplyPrefix()
        {
            return !SuppressParameterCompressor;
        }

        private static Type FindType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null) return type;
            }
            return null;
        }
    }
}
