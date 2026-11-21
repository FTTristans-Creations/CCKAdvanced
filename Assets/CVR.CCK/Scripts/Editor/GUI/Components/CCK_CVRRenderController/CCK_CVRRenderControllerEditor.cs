#if UNITY_EDITOR
    using ABI.CCK.Scripts.Editor;
    using CVR.CCKEditor.Localization;
    using UnityEditor;

    namespace ABI.CCK.Components
    {
        [CanEditMultipleObjects]
        [CustomEditor(typeof(CVRRenderController))]
        public partial class CCK_CVRRenderController : CCKBaseLocalizedEditor
        {
            #region Private Variables

            private CVRRenderController _CVRRenderController;

            #endregion

            #region Serialized Properties

            #endregion

            #region Unity Events

            protected override void OnEnable()
            {
                base.OnEnable();

                if (target == null) return;
                _CVRRenderController = (CVRRenderController)target;
            }

            protected override void OnDisable()
            {
                base.OnDisable();
            }

            public override void OnInspectorGUI()
            {
                if (_CVRRenderController == null)
                    return;

                DrawLocalizationWarning();
                
                using (new SharedComponentGUI.LabelScope(L("BlitterSettings").text))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRRenderController.cvrRenderComponents)), L("cvrRenderComponents"));
                       
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CVRRenderController.renderersToInstantiate)), L("renderersToInstantiate"));
                    }
                }

                int maxRenderTargets = 0;
                int lowestUAVSlot = 8;
                foreach (var cvrRenderComponent in _CVRRenderController.cvrRenderComponents)
                {
                    if (cvrRenderComponent != null)
                    {
                        if (cvrRenderComponent is CVRBlitter blitter)
                        {
                            if (blitter._MultiTargetBlitMode)
                            {
                                int count = blitter.destinationTextures.Length;
                                if (count > maxRenderTargets)
                                    maxRenderTargets = count;
                            }
                        }
                        else if (cvrRenderComponent is CVRMaterialDataProvider materialDataProvider)
                        {
                            if (materialDataProvider.dataMode == CVRMaterialDataProvider.DataMode.MeshIndexBuffer ||
                                materialDataProvider.dataMode == CVRMaterialDataProvider.DataMode.MeshVertexBuffer ||
                                materialDataProvider.dataMode == CVRMaterialDataProvider.DataMode.SkinnedMeshRendererVertexBuffer ||
                                materialDataProvider.dataMode == CVRMaterialDataProvider.DataMode.RenderTextureData)
                            {
                                if (materialDataProvider.dataType == CVRMaterialDataProvider.DataType.ReadWrite)
                                {
                                    int uavSlot = (int)materialDataProvider.uavSlot;
                                    if (uavSlot < lowestUAVSlot)
                                        lowestUAVSlot = uavSlot;
                                }
                            }
                        }
                    }
                }

                if (maxRenderTargets >= lowestUAVSlot)
                {
                    EditorGUILayout.HelpBox(L($"\nLowest Binding Index for UAV {lowestUAVSlot} must be higher than the max number of RenderTargets {maxRenderTargets}\n").text, MessageType.Error);
                }

                EditorGUILayout.Space();

                serializedObject.ApplyModifiedProperties();
            }

            #endregion
        }
    }
#endif