using CVR.CCKEditor.Localization;
using UnityEditor;
using UnityEngine;

#if LTCGI_INCLUDED
using pi.LTCGI;
#endif

namespace ABI.CCK.Components
{
    [CustomEditor(typeof(CVRLTCGIAdapter))]
    public partial class CCK_CVRLTCGIAdapterEditor : CCKBaseLocalizedEditor
    {
        #region Constants

        private const string KAFE_LTCGI_REPO_URL = "https://github.com/kafeijao/ltcgi/";
        private const string KAFE_LTCGI_GIT_URL = "https://github.com/kafeijao/ltcgi.git";
        private const string KAFE_LTCGI_GIT_RELEASES_URL = "https://github.com/kafeijao/ltcgi/releases/latest";

        private const string LTCGI_CONTROLLER_PREFAB_PATH = "Packages/at.pimaker.ltcgi/LTCGI Controller.prefab";

        #endregion Constants
        
        #region Editor GUI Foldouts

        private static bool _guiDebugFoldout;

        #endregion Editor GUI Foldouts

        #region Private Variables
        
        private CVRLTCGIAdapter _baseLTCGIAdapter;
        private static GameObject _controllerPrefab;

        #endregion Private Variables

        #region Unity Events

        protected override void OnEnable()
        {
            base.OnEnable();
            
            if (target == null) return;
            _baseLTCGIAdapter = (CVRLTCGIAdapter)target;
            LoadLTCGIControllerPrefab();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        private static void LoadLTCGIControllerPrefab()
        {
#if LTCGI_INCLUDED
            // Load the LTCGI controller prefab
            // Debug.Log($"Loading the LTCGI Controller Prefab from: {LTCGI_CONTROLLER_PREFAB_PATH}...");
            _controllerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LTCGI_CONTROLLER_PREFAB_PATH);
#endif
        }

        public override void OnInspectorGUI()
        {
            if (_baseLTCGIAdapter == null)
                return;

            DrawLocalizationWarning();
            
            serializedObject.Update();

            EditorGUILayout.HelpBox(L("LtcgiAdapterInfo").text, MessageType.Info);
            
#if LTCGI_INCLUDED
            Draw_HasLtcgi();
#else
            Draw_MissingLtcgi();
#endif
            // Draw_Debug();
            
            serializedObject.ApplyModifiedProperties();
        }

        #endregion Unity Events

        #region Drawing Methods

        private void Draw_HasLtcgi()
        {
#if LTCGI_INCLUDED
            LTCGI_Controller controller = LTCGI_Controller.Singleton;
            if (controller != null)
            {
                // draw field for the controller so user can ping it
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField(L("LtcgiController").text, controller, typeof(LTCGI_Controller), true);
                return;
            }

            EditorGUILayout.HelpBox(L("LtcgiControllerNotFoundWarning").text, MessageType.Warning);

            bool foundControllerPrefab = _controllerPrefab != null;
            using (new EditorGUI.DisabledScope(!foundControllerPrefab))
            {

                if (GUILayout.Button(L("CreateLtcgiController").text) && foundControllerPrefab)
                {
                    // Get the component from the prefab
                    LTCGI_Controller prefabController = _controllerPrefab.GetComponent<LTCGI_Controller>();
                    if (prefabController != null)
                    {
                        // Add the component to the current GameObject
                        LTCGI_Controller newController = _baseLTCGIAdapter.gameObject.AddComponent<LTCGI_Controller>();

                        // Copy the values from the prefab to the new component
                        UnityEditorInternal.ComponentUtility.CopyComponent(prefabController);
                        UnityEditorInternal.ComponentUtility.PasteComponentValues(newController);
                    }
                    else
                    {
                        Debug.LogError("Failed: LTCGI_Controller component not found on prefab");
                    }
                }
            }
            if (!foundControllerPrefab)
            {
                EditorGUILayout.HelpBox(string.Format(L("LtcgiControllerPrefabNotFoundWarning").text, LTCGI_CONTROLLER_PREFAB_PATH), MessageType.Warning);
            }

            #endif
        }

        #endregion Drawing Methods
    }
}