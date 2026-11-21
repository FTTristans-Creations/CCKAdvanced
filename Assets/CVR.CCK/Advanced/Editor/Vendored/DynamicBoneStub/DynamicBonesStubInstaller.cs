#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;

// Vendored copy of VRLabs' Dynamic-Bones-Stub installer (MIT, see LICENSE).
// Class renamed so it can coexist with the official VRLabs package; the file
// name is kept so AssetDatabase.FindAssets("DynamicBonesStubInstaller") still
// locates this copy's Scripts payload.
[InitializeOnLoad]
public class CCKDynamicBonesStubInstaller
{
    static CCKDynamicBonesStubInstaller()
    {
        bool fikStubPresent = AppDomain.CurrentDomain.GetAssemblies()
            .Any(x => x.GetTypes().Any(y => y.FullName == "DynamicBone"));
        bool anythingInLocation = Directory.Exists("Assets/DynamicBone/Scripts");
        if (fikStubPresent && !anythingInLocation)
        {

            EditorUtility.DisplayDialog("Bad Dynamic Bones Install", "Dynamic Bones Found in wrong folder. Please move your Dynamic Bones/Dynamic Bones Stub over to Assets/DynamicBone/Scripts", "Ok");
        }

        if (!fikStubPresent && !anythingInLocation)
        {
            string file = AssetDatabase.FindAssets("DynamicBonesStubInstaller", new[] { "Packages", "Assets" }).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();

            if (file == null)
            {
                EditorUtility.DisplayDialog("Bad Dynamic Bones Install", "Dynamic Bones Stub not found. Please install Dynamic Bones Stub from the package manager", "Ok");
                return;
            }

            string[] files = Directory.GetFiles(Path.GetDirectoryName(file) + "/Scripts", "*.*", SearchOption.AllDirectories);
            CopyFiles(files, "Assets/DynamicBone/");
            InstallAsmdef();
        }
    }


    // Package scripts cannot see Assembly-CSharp, so the stub needs its own
    // assembly for the CCK's DynamicBone references to resolve.
    private static void InstallAsmdef()
    {
        const string asmdefPath = "Assets/DynamicBone/DynamicBone.asmdef";
        if (!File.Exists(asmdefPath))
        {
            File.WriteAllText(asmdefPath,
                "{\n" +
                "    \"name\": \"DynamicBone\",\n" +
                "    \"rootNamespace\": \"\",\n" +
                "    \"references\": [],\n" +
                "    \"includePlatforms\": [],\n" +
                "    \"excludePlatforms\": [],\n" +
                "    \"allowUnsafeCode\": false,\n" +
                "    \"overrideReferences\": false,\n" +
                "    \"precompiledReferences\": [],\n" +
                "    \"autoReferenced\": true,\n" +
                "    \"defineConstraints\": [],\n" +
                "    \"versionDefines\": [],\n" +
                "    \"noEngineReferences\": false\n" +
                "}\n");
        }
        if (!File.Exists(asmdefPath + ".meta"))
        {
            File.WriteAllText(asmdefPath + ".meta",
                "fileFormatVersion: 2\n" +
                "guid: e5f6117603f1462ab4c7b8862beca0d7\n" +
                "AssemblyDefinitionImporter:\n" +
                "  externalObjects: {}\n" +
                "  userData: \n" +
                "  assetBundleName: \n" +
                "  assetBundleVariant: \n");
        }
        AssetDatabase.Refresh();
    }


    private static void CopyFiles(string[] files, string finalPath)
    {
        foreach (var file in files)
        {
            if (file.EndsWith(".cs.no"))
            {
                string partialPath = file.Substring(file.IndexOf("Scripts", StringComparison.Ordinal));
                string directory = Path.GetDirectoryName(partialPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(finalPath + directory);
                }

                File.Copy(file, finalPath + partialPath.Replace(".cs.no", ".cs"));
                File.Copy(file.Replace(".cs.no", ".cs.meta.no"), finalPath + partialPath.Replace(".cs.no", ".cs.meta"));
            }
        }
    }
}

#endif
