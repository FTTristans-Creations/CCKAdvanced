/*using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using ABI.CCK.Components;
using ABI.CCK.Scripts.Runtime;
using CVR.CCKEditor.API;
using UnityEditor;
using UnityEngine;

namespace ABI.CCK.Scripts.Editor
{
    public partial class CCK_BuildManagerWindow : EditorWindow
    {
        private string _inputUsername;
        private string _inputAccessKey;

        public Texture2D abiLogo;
        
        private int _tab;
        private Vector2 _scroll;

        private static PropertyInfo _legacyBlendShapeImporter;
        
        private static PropertyInfo legacyBlendShapeImporter
        {
            get
            {
                if(_legacyBlendShapeImporter != null)
                {
                    return _legacyBlendShapeImporter;
                }

                Type modelImporterType = typeof(ModelImporter);
                _legacyBlendShapeImporter = modelImporterType.GetProperty(
                    "legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );

                return _legacyBlendShapeImporter;
            }
        }
        
        [MenuItem("ChilloutVR CCK/Control Panel", false, 200)]
        private static void Init()
        {
            CCK_BuildManagerWindow window = (CCK_BuildManagerWindow)GetWindow(typeof(CCK_BuildManagerWindow), false, $"CCK :: Control Panel");
            window.Show();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnEditorStateUpdated;
        }

        private void OnEnable()
        {
            abiLogo = (Texture2D) AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("0d770be82f9f8c24f8ce828100c873ee"), typeof(Texture2D));
            
            EditorApplication.playModeStateChanged -= OnEditorStateUpdated;
            EditorApplication.playModeStateChanged += OnEditorStateUpdated;
            
            ApiConnection.LoginIfNeeded();
        }

        private void OnEditorStateUpdated(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorPrefs.SetBool("m_ABI_isBuilding", false);
                EditorPrefs.SetString("m_ABI_TempVersion", CVRCommon.CCK_VERSION);
                
                string[] filePaths;
                string cacheDir = Application.dataPath + "/CCKBuildCache/Resources/Cache/";
                if (Directory.Exists(cacheDir))
                {
                    filePaths = Directory.GetFiles(cacheDir, "*.prefab");
                    foreach (string filePath in filePaths)
                    {
                        File.Delete(filePath);
                    }
                }
                
                filePaths = Directory.GetFiles(Application.persistentDataPath + "/", "*.cvravatar");
                foreach (string filePath in filePaths)
                {
                    File.Delete(filePath);
                }
                
                filePaths = Directory.GetFiles(Application.persistentDataPath + "/", "*.cvravatar.manifest");
                foreach (string filePath in filePaths)
                {
                    File.Delete(filePath);
                }
                
                filePaths = Directory.GetFiles(Application.persistentDataPath + "/", "*.cvrprop");
                foreach (string filePath in filePaths)
                {
                    File.Delete(filePath);
                }
                
                filePaths = Directory.GetFiles(Application.persistentDataPath + "/", "*.cvrprop.manifest");
                foreach (string filePath in filePaths)
                {
                    File.Delete(filePath);
                }
                
                if (File.Exists(Application.persistentDataPath + "/bundle.cvrworld")) File.Delete(Application.persistentDataPath + "/bundle.cvrworld");
                if (File.Exists(Application.persistentDataPath + "/bundle.cvrworld.manifest")) File.Delete(Application.persistentDataPath + "/bundle.cvrworld.manifest");
                if (File.Exists(Application.persistentDataPath + "/bundle.png")) File.Delete(Application.persistentDataPath + "/bundle.png");
                if (File.Exists(Application.persistentDataPath + "/bundle.png.manifest")) File.Delete(Application.persistentDataPath + "/bundle.png.manifest");
                if (File.Exists(Application.persistentDataPath + "/bundle_pano_1024.png")) File.Delete(Application.persistentDataPath + "/bundle_pano_1024.png");
                if (File.Exists(Application.persistentDataPath + "/bundle_pano_1024.png.manifest")) File.Delete(Application.persistentDataPath + "/bundle_pano_1024.png.manifest");
                if (File.Exists(Application.persistentDataPath + "/bundle_pano_4096.png")) File.Delete(Application.persistentDataPath + "/bundle_pano_4096.png");
                if (File.Exists(Application.persistentDataPath + "/bundle_pano_4096.png.manifest")) File.Delete(Application.persistentDataPath + "/bundle_pano_4096.png.manifest");
                AssetDatabase.Refresh();
            }

            if (state == PlayModeStateChange.EnteredPlayMode && EditorPrefs.GetBool("m_ABI_isBuilding"))
            {
                CCK_BuildUtility.upload_id = EditorPrefs.GetString("m_ABI_uploadId");
                string randomNum = EditorPrefs.GetString("m_ABI_uploadRand");
                
                GameObject ui = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath("fb59cb45c21334f42bb4dda20f492b65")));
                OnGuiUpdater up = ui.GetComponentInChildren<OnGuiUpdater>();
                if (File.Exists(Application.dataPath + "/CCKBuildCache/Resources/Cache/_CVRAvatar.prefab"))up.asset = Resources.Load<GameObject>("Cache/_CVRAvatar").GetComponent<CVRAssetInfo>();
                if (File.Exists(Application.dataPath + $"/CCKBuildCache/Resources/Cache/CVRAvatar_{CCK_BuildUtility.upload_id}_{randomNum}.prefab"))up.asset = Resources.Load<GameObject>($"Cache/CVRAvatar_{CCK_BuildUtility.upload_id}_{randomNum}").GetComponent<CVRAssetInfo>();
                if (File.Exists(Application.dataPath + "/CCKBuildCache/Resources/Cache/_CVRSpawnable.prefab"))up.asset = Resources.Load<GameObject>("Cache/_CVRSpawnable").GetComponent<CVRAssetInfo>();
                if (File.Exists(Application.dataPath + $"/CCKBuildCache/Resources/Cache/CVRSpawnable_{CCK_BuildUtility.upload_id}_{randomNum}.prefab"))up.asset = Resources.Load<GameObject>($"Cache/CVRSpawnable_{CCK_BuildUtility.upload_id}_{randomNum}").GetComponent<CVRAssetInfo>();
                if (File.Exists(Application.dataPath + "/CCKBuildCache/Resources/Cache/_CVRWorld.prefab"))up.asset = Resources.Load<GameObject>("Cache/_CVRWorld").GetComponent<CVRAssetInfo>();
            }
        }

        private void OnGUI()
        {
            GUIStyle centeredStyle = new()
            {
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 70, // Fixes size when Android
            };

            GUILayout.Label(abiLogo, centeredStyle);
            GUILayout.Label("CCK Version: " + CVRCommon.CCK_VERSION_FULL);
            EditorGUILayout.BeginVertical();
            
            _tab = GUILayout.Toolbar(_tab, new[] {
                "Community Hub",
                CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_HEADING_BUILDER"),
                CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_HEADING_SETTINGS")
            });

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            
            switch (_tab)
            {
                case 0:
                    if (!ApiConnection.IsLoggedIn)
                        Tab_LogIn();
                    else
                        Tab_CommunityHub();
                    break;
                case 1:
                    Tab_FoundContent();
                    break;
                case 2:
                    Tab_Settings();
                    break;
            }
            
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
            
            // EditorGUILayout.BeginVertical();
            // GUILayout.FlexibleSpace();
            // EditorGUILayout.LabelField("CCK version    ", CVRCommon.CCK_VERSION_FULL);
            // EditorGUILayout.LabelField("Unity version  ", $"{Application.unityVersion}");
            // EditorGUILayout.EndVertical();
        }
        
        private static void Logout()
        {
            ApiConnection.Logout();
            ApiCredentialsHandler.ClearCredentials();
        }

        private async Task AttemptLoginWithCredentials()
        {
            if (ApiConnection.AuthState == ApiConnection.AuthenticationState.AttemptingLogin)
                return;
            
            if (!string.IsNullOrEmpty(_inputUsername) && !string.IsNullOrEmpty(_inputAccessKey))
            {
                ApiConnection.Logout();
                ApiCredentialsHandler.SaveCredentials(_inputUsername, _inputAccessKey);
            }
            
            await ApiConnection.Login();
            
            Repaint();
        }
    }
}*/