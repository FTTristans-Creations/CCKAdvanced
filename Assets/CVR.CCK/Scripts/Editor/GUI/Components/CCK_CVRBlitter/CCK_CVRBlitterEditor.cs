#if UNITY_EDITOR
    using ABI.CCK.Scripts.Editor;
    using CVR.CCKEditor.Localization;
    using UnityEditor;

    namespace ABI.CCK.Components
    {
        [CanEditMultipleObjects]
        [CustomEditor(typeof(CVRBlitter))]
        public partial class CCK_CVRBlitterEditor : CCKBaseLocalizedEditor
        {
            #region Private Variables

            private CVRBlitter _blitter;

            #endregion

            #region Serialized Properties

            #endregion

            #region Unity Events

            protected override void OnEnable()
            {
                base.OnEnable();

                if (target == null) return;
                _blitter = (CVRBlitter)target;
            }

            protected override void OnDisable()
            {
                base.OnDisable();
            }

            public override void OnInspectorGUI()
            {
                if (_blitter == null)
                    return;

                DrawLocalizationWarning();
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRBlitter._MultiTargetBlitMode)), L("UseMultiTargetBlitMode"));
                
                using (new SharedComponentGUI.LabelScope(L("BlitterSettings").text))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRBlitter.originTexture)), L("OriginTexture"));
                        if(!_blitter._MultiTargetBlitMode)
                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRBlitter.destinationTexture)), L("DestinationTexture"));
                        else
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRBlitter.destinationTextures)), L("DestinationTextures"));
                            if (_blitter.destinationTextures.Length > 8)
                            {
                                EditorGUILayout.HelpBox(L("\nOnly a maximum of 8 render targets are supported\n").text, MessageType.Error);
                            }
                        }
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRBlitter.blitMaterial)), L("BlitMaterial"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRBlitter.clearEveryFrame)), L("ClearEveryFrame"));
                    }
                }

                EditorGUILayout.Space();

                serializedObject.ApplyModifiedProperties();
            }

            #endregion
        }
    }
#endif