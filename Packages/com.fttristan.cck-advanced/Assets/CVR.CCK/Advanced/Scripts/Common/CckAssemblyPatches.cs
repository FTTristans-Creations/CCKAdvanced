using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEditor;
using UnityEngine;

namespace CVR.CCKEditor.Advanced
{
    /// <summary>
    /// Third-party editor tools locate CCK types by reflection through hardcoded
    /// assembly names ("Assembly-CSharp" / "Assembly-CSharp-Editor"). Now that the
    /// CCK ships as the CVR.CCK asmdef those lookups return null and integrations
    /// such as ThryEditor's material auto-lock and upload auto-anchor fail.
    ///
    /// Patching Assembly.GetType directly is too dangerous (it is one of the
    /// hottest methods in the CLR and destabilized the editor), and patching the
    /// offending static constructors is impossible because preparing the detour
    /// runs the constructor. Instead we postfix Assembly.Load(string): when the
    /// predefined assemblies are requested we return a thin facade whose GetType
    /// falls back to searching every loaded assembly — but only for CCK type
    /// names, so unrelated lookups keep their exact semantics.
    /// </summary>
    [InitializeOnLoad]
    internal static class CckAssemblyPatches
    {
        private const string HarmonyId = "com.fttristan.cck-advanced.cck-assembly";

        private static readonly Dictionary<Assembly, Assembly> Facades = new();
        private static readonly object FacadeLock = new();
        private static bool _applied;

        [ModuleInitializer]
        internal static void ModuleInit()
        {
            TryApply();
        }

        static CckAssemblyPatches()
        {
            TryApply();
        }

        // Disabled: Harmony-patching core System.Reflection entry points during
        // editor startup repeatedly crashed Unity (SIGSEGV inside VRCFury's init).
        // Kept for reference; re-enable only after finding a safe injection point.
        private const bool Enabled = false;

        private static void TryApply()
        {
            if (_applied || !Enabled)
            {
                return;
            }

            try
            {
                var original = typeof(Assembly).GetMethod(
                    nameof(Assembly.Load),
                    new[] { typeof(string) });
                if (original == null)
                {
                    return;
                }

                var postfix = typeof(CckAssemblyPatches).GetMethod(
                    nameof(LoadPostfix),
                    BindingFlags.Static | BindingFlags.NonPublic);

                new Harmony(HarmonyId).Patch(original, postfix: new HarmonyMethod(postfix));
                _applied = true;
                Debug.Log("[CCK Advanced] assembly facade patch applied");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CCK Advanced] Failed to apply assembly facade patch: {e.Message}");
            }
        }

        private static void LoadPostfix(ref Assembly __result)
        {
            if (__result == null)
            {
                return;
            }

            string name;
            try
            {
                name = __result.GetName().Name;
            }
            catch
            {
                return;
            }

            if (name != "Assembly-CSharp" && name != "Assembly-CSharp-Editor"
                && name != "Assembly-CSharp-firstpass")
            {
                return;
            }

            lock (FacadeLock)
            {
                if (!Facades.TryGetValue(__result, out var facade))
                {
                    facade = new CckAssemblyFacade(__result);
                    Facades[__result] = facade;
                }

                __result = facade;
            }
        }

        internal static Type FindCckType(string name, Assembly skip)
        {
            if (string.IsNullOrEmpty(name) || !IsCckTypeName(name))
            {
                return null;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == skip)
                {
                    continue;
                }

                try
                {
                    var type = assembly.GetType(name, false);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch
                {
                    // Assembly still loading; skip it.
                }
            }

            return null;
        }

        private static bool IsCckTypeName(string name)
        {
            return name.StartsWith("ABI.CCK.", StringComparison.Ordinal)
                || name.StartsWith("CCK.CE.", StringComparison.Ordinal)
                || name.StartsWith("CVR.CCK", StringComparison.Ordinal);
        }

        /// <summary>
        /// Delegating Assembly wrapper returned for the predefined assemblies.
        /// Every member forwards to the real assembly except GetType, which adds
        /// a CCK-only fallback search across loaded assemblies.
        /// </summary>
        private sealed class CckAssemblyFacade : Assembly
        {
            private readonly Assembly _inner;

            public CckAssemblyFacade(Assembly inner)
            {
                _inner = inner;
            }

            public override Type GetType(string name)
            {
                return _inner.GetType(name) ?? FindCckType(name, _inner);
            }

            public override Type GetType(string name, bool throwOnError)
            {
                var type = _inner.GetType(name, false) ?? FindCckType(name, _inner);
                if (type == null && throwOnError)
                {
                    // Re-issue through the inner assembly to get the standard exception.
                    type = _inner.GetType(name, true);
                }

                return type;
            }

            public override Type GetType(string name, bool throwOnError, bool ignoreCase)
            {
                var type = _inner.GetType(name, false, ignoreCase) ?? FindCckType(name, _inner);
                if (type == null && throwOnError)
                {
                    type = _inner.GetType(name, true, ignoreCase);
                }

                return type;
            }

            public override string FullName => _inner.FullName;
            public override string CodeBase => _inner.CodeBase;
            public override string EscapedCodeBase => _inner.EscapedCodeBase;
            public override string Location => _inner.Location;
            public override bool GlobalAssemblyCache => _inner.GlobalAssemblyCache;
            public override bool ReflectionOnly => _inner.ReflectionOnly;
            public override Type[] GetTypes() => _inner.GetTypes();
            public override Type[] GetExportedTypes() => _inner.GetExportedTypes();
            public override MethodInfo EntryPoint => _inner.EntryPoint;
            public override AssemblyName GetName() => _inner.GetName();
            public override AssemblyName GetName(bool copiedName) => _inner.GetName(copiedName);
            public override AssemblyName[] GetReferencedAssemblies() => _inner.GetReferencedAssemblies();
            public override string[] GetManifestResourceNames() => _inner.GetManifestResourceNames();
            public override ManifestResourceInfo GetManifestResourceInfo(string resourceName) => _inner.GetManifestResourceInfo(resourceName);
            public override Stream GetManifestResourceStream(string name) => _inner.GetManifestResourceStream(name);
            public override FileStream GetFile(string path) => _inner.GetFile(path);
            public override FileStream[] GetFiles() => _inner.GetFiles();
            public override FileStream[] GetFiles(bool getResourceModules) => _inner.GetFiles(getResourceModules);
            public override Module ManifestModule => _inner.ManifestModule;
            public override object[] GetCustomAttributes(bool inherit) => _inner.GetCustomAttributes(inherit);
            public override object[] GetCustomAttributes(Type attributeType, bool inherit) => _inner.GetCustomAttributes(attributeType, inherit);
            public override bool IsDefined(Type attributeType, bool inherit) => _inner.IsDefined(attributeType, inherit);
            public override IList<CustomAttributeData> GetCustomAttributesData() => _inner.GetCustomAttributesData();
            public override Assembly GetSatelliteAssembly(CultureInfo culture) => _inner.GetSatelliteAssembly(culture);
            public override Assembly GetSatelliteAssembly(CultureInfo culture, Version version) => _inner.GetSatelliteAssembly(culture, version);
        }
    }
}

// Unity's profile targets netstandard2.1 which lacks ModuleInitializerAttribute.
// Declaring it ourselves lets the compiler emit a module initializer that Mono
// runs when the assembly is loaded — before any [InitializeOnLoad] code.
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class ModuleInitializerAttribute : Attribute
    {
    }
}
