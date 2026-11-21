using System.IO;
using UnityEditor;
using UnityEngine;

namespace CVR.CCKEditor.Advanced
{
    /// <summary>
    /// Third-party tools locate the CCK avatar descriptor via
    /// Assembly.Load("Assembly-CSharp").GetType("ABI.CCK.Components.CVRAvatar"),
    /// which broke when the CCK moved into the CVR.CCK asmdef. Unity does not
    /// compile runtime package scripts into Assembly-CSharp, so the stub cannot
    /// ship inside the package — instead we install it into the project's Assets
    /// on load whenever it is missing or differs.
    /// </summary>
    [InitializeOnLoad]
    internal static class AssemblyCSharpStubInstaller
    {
        private const string StubDirectory = "Assets/CCKStubs";
        private const string StubPath = StubDirectory + "/CVRAvatar.cs";

        private const string StubSource = @"using UnityEngine;

namespace ABI.CCK.Components
{
    // Reflection compatibility stub installed by CCK Advanced.
    // Third-party tools resolve ABI.CCK.Components.CVRAvatar via
    // Assembly.Load(""Assembly-CSharp"").GetType(...) which requires the type to
    // exist in Assembly-CSharp. This stub satisfies that lookup; the real
    // CVRAvatar lives in the CVR.CCK assembly. Do not delete this file unless
    // you know what you are doing - it is reinstalled automatically.
    internal class CVRAvatar : MonoBehaviour
    {
        public AnimatorOverrideController overrides;
    }
}
";

        static AssemblyCSharpStubInstaller()
        {
            try
            {
                EnsureStub();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CCK Advanced] Failed to install Assembly-CSharp stub: {e.Message}");
            }
        }

        private static void EnsureStub()
        {
            var existing = File.Exists(StubPath) ? File.ReadAllText(StubPath) : null;
            if (existing != null && Normalize(existing) == Normalize(StubSource))
            {
                return;
            }

            Directory.CreateDirectory(StubDirectory);
            File.WriteAllText(StubPath, StubSource);
            AssetDatabase.Refresh();
            Debug.Log("[CCK Advanced] Installed Assets/CCKStubs/CVRAvatar.cs for third-party CCK compatibility");
        }

        private static string Normalize(string text)
        {
            return text.Replace("\r\n", "\n").TrimEnd();
        }
    }
}
