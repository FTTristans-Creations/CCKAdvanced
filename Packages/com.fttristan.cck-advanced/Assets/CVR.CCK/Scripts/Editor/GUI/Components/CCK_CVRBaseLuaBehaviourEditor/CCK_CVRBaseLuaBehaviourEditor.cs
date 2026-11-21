using ABI.CCK.Scripts.Editor;
using CVR.CCKEditor.Localization;
using UnityEditor;

namespace ABI.CCK.Components
{
    [CustomEditor(typeof(CVRLuaClientBehaviour))]
    public partial class CCK_CVRLuaClientBehaviourEditor : CCKBaseLocalizedEditor
    {
        #region Editor GUI Fields

        private static bool _guiLuaAssetFoldout = true;
        private static bool _guiBoundObjectsFoldout = true;

        #endregion
        
        #region Private Variables
        
        private CVRBaseLuaBehaviour _baseLuaBehaviour;
        
        #endregion

        #region Serialized Properties
        private SerializedProperty m_LocalOnly;
        private SerializedProperty m_LuaAsset;
        private SerializedProperty m_BoundObjects;

        #endregion

        protected override void OnEnable()
        {
            base.OnEnable();
            
            if (target == null) return;
            _baseLuaBehaviour = (CVRLuaClientBehaviour)target;

            m_LocalOnly = serializedObject.FindProperty(nameof(CVRLuaClientBehaviour.localOnly));
            m_LuaAsset = serializedObject.FindProperty(nameof(CVRLuaClientBehaviour.asset));
            m_BoundObjects = serializedObject.FindProperty(nameof(CVRLuaClientBehaviour.boundObjects));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }
        
        public override void OnInspectorGUI()
        {
            if (_baseLuaBehaviour == null)
                return;

            DrawLocalizationWarning();

            EditorGUILayout.HelpBox(L("LuaInfo").text, MessageType.Info);
            
            EditorGUILayout.HelpBox(L("LuaNightlyWarning").text, MessageType.Warning);
            
            serializedObject.Update();

            Draw_LuaAsset();
            Draw_BoundObjects();
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}