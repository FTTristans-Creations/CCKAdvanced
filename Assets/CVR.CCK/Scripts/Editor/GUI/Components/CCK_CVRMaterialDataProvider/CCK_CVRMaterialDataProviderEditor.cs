#if UNITY_EDITOR
    using ABI.CCK.Scripts.Editor;
    using CVR.CCKEditor.Localization;
    using UnityEditor;

    namespace ABI.CCK.Components
    {
        [CanEditMultipleObjects]
        [CustomEditor(typeof(CVRMaterialDataProvider))]
        public partial class CCK_CVRMaterialDataProviderEditor : CCKBaseLocalizedEditor
        {
            private CVRMaterialDataProvider _materialDataProvider;
            private static bool _guiDebugFoldout = false;
            private bool _isreferenced;

            protected override void OnEnable()
            {
                base.OnEnable();

                if (target == null) return;
                _materialDataProvider = (CVRMaterialDataProvider)target;

                var renderControllers =
                    FindObjectsByType<CVRRenderController>
                    (
                        UnityEngine.FindObjectsInactive.Include,
                        UnityEngine.FindObjectsSortMode.None
                    );

                foreach (var renderController in renderControllers)
                {
                    if (renderController.cvrRenderComponents.Contains(_materialDataProvider))
                    {
                        _isreferenced = true;
                        break;
                    }
                }
            }

            protected override void OnDisable()
            {
                base.OnDisable();
            }

            public override void OnInspectorGUI()
            {
                if (_materialDataProvider == null)
                    return;

                DrawLocalizationWarning();

                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.dataMode)), L("Data Source Type"));
                EditorGUILayout.Space(10);
                using (new SharedComponentGUI.LabelScope(L("Settings").text))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        switch (_materialDataProvider.dataMode)
                        {
                            case CVRMaterialDataProvider.DataMode.TransformWorldToLocal:
                            case CVRMaterialDataProvider.DataMode.TransformLocalToWorld:
                            case CVRMaterialDataProvider.DataMode.LastFrameTransformWorldToLocal:
                            case CVRMaterialDataProvider.DataMode.LastFrameTransformLocalToWorld:
                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.shaderPropertyName)), L("Shader Property Name"));
                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.materials)), L("Materials To Populate"));
                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.transforms)), L("Transforms To Use"));
                                break;
                            case CVRMaterialDataProvider.DataMode.MeshIndexBuffer:
                            case CVRMaterialDataProvider.DataMode.MeshVertexBuffer:
                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.dataType)), L("Type of Access"));
                                using (new SharedComponentGUI.LabelScope(L("Write Settings").text))
                                {
                                    using (new EditorGUI.IndentLevelScope())
                                    {
                                        switch (_materialDataProvider.dataType)
                                        {
                                            case CVRMaterialDataProvider.DataType.ReadOnly:
                                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.shaderPropertyName)), L("Shader Property Name"));
                                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.materials)), L("Materials To Populate"));
                                                break;
                                            case CVRMaterialDataProvider.DataType.ReadWrite:
                                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.uavSlot)), L("UAV Register"));
                                                break;
                                        }
                                    }
                                }

                                EditorGUILayout.Space(5);
                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.Renderer)), L("Renderer To Use"));
                                break;
                            
                            case CVRMaterialDataProvider.DataMode.SkinnedMeshRendererPreviousVertexBuffer:

                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.shaderPropertyName)), L("Shader Property Name"));
                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.materials)), L("Materials To Populate"));

                                EditorGUILayout.Space(5);
                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.smr)), L("Renderer To Use"));
                                break;

                            case CVRMaterialDataProvider.DataMode.SkinnedMeshRendererVertexBuffer:
                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.dataType)), L("Type of Access"));
                                using (new SharedComponentGUI.LabelScope(L("Write Settings").text))
                                {
                                    using (new EditorGUI.IndentLevelScope())
                                    {
                                        switch (_materialDataProvider.dataType)
                                        {
                                            case CVRMaterialDataProvider.DataType.ReadOnly:
                                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.shaderPropertyName)), L("Shader Property Name"));
                                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.materials)), L("Materials To Populate"));
                                                break;
                                            case CVRMaterialDataProvider.DataType.ReadWrite:
                                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.uavSlot)), L("UAV Register"));
                                                break;
                                        }
                                    }
                                }

                                EditorGUILayout.Space(5);
                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.smr)), L("Renderer To Use"));
                                break;

                            case CVRMaterialDataProvider.DataMode.RenderTextureData:
                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.dataType)), L("Type of Access"));
                                using (new SharedComponentGUI.LabelScope(L("Write Settings").text))
                                {
                                    using (new EditorGUI.IndentLevelScope())
                                    {
                                        switch (_materialDataProvider.dataType)
                                        {
                                            case CVRMaterialDataProvider.DataType.ReadOnly:
                                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.shaderPropertyName)), L("Shader Property Name"));
                                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.materials)), L("Materials To Populate"));
                                                break;
                                            case CVRMaterialDataProvider.DataType.ReadWrite:
                                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.uavSlot)), L("UAV Register"));
                                                break;
                                        }
                                    }
                                }

                                EditorGUILayout.Space(5);
                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.rt)), L("RenderTexture to Use"));
                                break;
                        }
                    }
                }

                using (new SharedComponentGUI.FoldoutScope(ref _guiDebugFoldout, L("Debug Information").text))
                {
                    if (_guiDebugFoldout)
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.BufferSize)), L("Buffer Size (Bytes)"));
                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRMaterialDataProvider.StrideSize)), L("Stride Size (Bytes)"));
                        }
                    }
                }

                serializedObject.ApplyModifiedProperties();
                if (!_isreferenced)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.HelpBox(L("\nThis component is not referenced by any CVR Render Controller in the scene. It will not function until it is added to one.\n").text, MessageType.Error);
                }
            }
        }
    }
#endif