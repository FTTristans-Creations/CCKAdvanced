#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using ABI.CCK.Components;
using CVR.CCKEditor.API;
using CVR.CCKEditor.ContentBuilder;
using CVR.CCKEditor.TestMode;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

public partial class BuilderTab
{
    private class ContentSelectionSection : BuilderUISection
    {
        private readonly DropdownField _contentSelector;
        private readonly Button _contentUtility;

        private readonly VisualElement _contentInfoContainer;
        private readonly VisualElement _contentErrorContainer;
        
        private readonly VisualElement _noContentSection;
        private readonly VisualElement _hasContentSection;
        
        private CVRAssetInfo[] _cachedContent;

        public event Action<CVRAssetInfo> OnSelectionChanged;

        public ContentSelectionSection(VisualElement root, BuilderState state) : base(root, state)
        {
            _contentSelector = Root.Q<DropdownField>("ContentSelector");
            _noContentSection = Root.Q("NoContent");
            _hasContentSection = Root.Q("HasContent");
            
            _contentInfoContainer = Root.Q("ContentDetails");
            _contentErrorContainer = Root.Q("ContentErrorDetails");
            
            _contentUtility = Root.Q<Button>("button-content-utility");

            _contentSelector.RegisterValueChangedCallback(_ =>
            {
                if (_cachedContent != null) OnSelectionChanged?.Invoke(_cachedContent[_contentSelector.index]);
            });

            _contentUtility.clicked += () =>
            {
                CVRAssetInfo selectedContent = null;
                bool hasSelection = _cachedContent != null &&
                                    _contentSelector.index >= 0 &&
                                    _contentSelector.index < _cachedContent.Length;

                if (hasSelection) selectedContent = _cachedContent[_contentSelector.index];

                bool hasGuid = hasSelection && !string.IsNullOrEmpty(selectedContent.objectId);

                GenericMenu menu = new();
                
                menu.AddItem(new GUIContent("Refetch Content Details"), false,
                    hasSelection ? () =>
                        {
                            OnSelectionChanged?.Invoke(null);
                            OnSelectionChanged?.Invoke(selectedContent);
                        }
                        : null);
                
                menu.AddSeparator(string.Empty);

                menu.AddItem(new GUIContent("Ping Object in Scene"), false,
                    hasSelection ? () => EditorGUIUtility.PingObject(selectedContent.gameObject) : null);
                
                menu.AddSeparator(string.Empty);

                menu.AddItem(new GUIContent("Open in Browser"), false,
                    hasGuid ? () => Application.OpenURL(CVRApiContent.GetHubUrlForContent((ContentTypes)selectedContent.type, selectedContent.objectId)) : null);

                menu.AddItem(new GUIContent("Open in ChilloutVR"), false,
                    hasGuid ? () => Application.OpenURL(CVRApiContent.GetClientUrlForContent((ContentTypes)selectedContent.type, selectedContent.objectId)) : null);

                menu.AddSeparator(string.Empty);
                
                menu.AddItem(new GUIContent("Copy GUID"), false,
                    hasGuid ? () => EditorGUIUtility.systemCopyBuffer = selectedContent.objectId : null);

                menu.AddItem(new GUIContent("Clear GUID"), false,
                    hasGuid ? () =>
                    {
                        if (!EditorUtility.DisplayDialog(
                                "CCK :: Clear Attached GUID",
                                "Are you sure you want to clear the attached GUID?",
                                "Yes", "No")) return;

                        Undo.RecordObject(selectedContent, "Clear GUID");
                        selectedContent.objectId = string.Empty;

                        OnSelectionChanged?.Invoke(null);
                        OnSelectionChanged?.Invoke(selectedContent);
                    } : null);
                
                menu.AddSeparator(string.Empty);

                if (Application.isPlaying)
                {
                    // I know this is silly but I tried to use this menu to exit... so just replacing the menu item.
                    menu.AddItem(new GUIContent("Exit PlayMode"), false, 
                        () => EditorApplication.isPlaying = false);
                }
                else
                {
                    menu.AddItem(new GUIContent("Test in PlayMode"), false, 
                        hasSelection ? () => CCKTestModeManager.TestContent(selectedContent) : null);
                }

                menu.ShowAsContext();
            };
                
            // Quick Create
            Root.Q<Button>("btn-create-avatar").clicked += () => Debug.Log("TODO: Create Avatar");
            Root.Q<Button>("btn-create-prop").clicked += () => Debug.Log("TODO: Create Prop");
            Root.Q<Button>("btn-create-world").clicked += () => Debug.Log("TODO: Create World");
            Root.Q<Button>("btn-learn-more").clicked += () => Application.OpenURL("https://docs.abinteractive.net/cck/");
        }
        
        public void UpdateContentList(CVRAssetInfo[] assetInfos)
        {
            _cachedContent = assetInfos;
            
            bool anyContent = assetInfos.Length > 0;
            List<string> contentNames = null;
            
            if (anyContent)
            {
                contentNames = new List<string>(assetInfos.Length);
                foreach (CVRAssetInfo assetInfo in assetInfos)
                    contentNames.Add(GetContentDisplayName(assetInfo));

                _contentSelector.choices = contentNames;
            }

            _noContentSection.style.display = anyContent ? DisplayStyle.None : DisplayStyle.Flex;
            _hasContentSection.style.display = anyContent ? DisplayStyle.Flex : DisplayStyle.None;

            // Find the saved content in the list
            if (anyContent && !string.IsNullOrEmpty(State.SelectedLocalIdentifier))
            {
                for (var index = 0; index < assetInfos.Length; index++)
                {
                    var assetInfo = assetInfos[index];
                    if (assetInfo.localEditorIdentifier != State.SelectedLocalIdentifier) 
                        continue;
                    
                    _contentSelector.SetValueWithoutNotify(contentNames[index]);
                    return;
                }
            }

            // Fallback to first item if we can't restore previous selection
            if (anyContent) _contentSelector.value = contentNames[0];
        }

        public void PopulateFields(CVRApiContent content)
        {
            bool showError = content is { IsContentMine: false };
            
            _contentInfoContainer.style.display = showError ? DisplayStyle.None : DisplayStyle.Flex;
            _contentErrorContainer.style.display = showError ? DisplayStyle.Flex : DisplayStyle.None;
            Button detatchGuid = _contentErrorContainer.Q<Button>("button-detach-guid");
            detatchGuid.clicked += () =>
            {
                CVRAssetInfo selectedContent = null;
                bool hasSelection = _cachedContent != null &&
                                    _contentSelector.index >= 0 &&
                                    _contentSelector.index < _cachedContent.Length;

                if (hasSelection) selectedContent = _cachedContent[_contentSelector.index];
                if (!selectedContent)
                    return;
                
                if (!EditorUtility.DisplayDialog(
                        "CCK :: Detach GUID",
                        "Are you sure you want to detach the GUID from this content?",
                        "Yes", "No")) return;

                Undo.RecordObject(selectedContent, "Detach GUID");
                selectedContent.objectId = string.Empty;
                
                OnSelectionChanged?.Invoke(null);
                OnSelectionChanged?.Invoke(selectedContent);
            };
            
            // _contentErrorContainer.Q<Label>("ContentErrorMessage").text = content.ErrorMessage;
        }

        public override void LockInterface(bool locked) => _contentSelector.SetEnabled(!locked);

        public override void ClearFields()
        {
            // a
        }

        private static string GetContentDisplayName(CVRAssetInfo info)
        {
            string prefix = info.type switch
            {
                CVRAssetInfo.AssetType.Avatar => "Avatar",
                CVRAssetInfo.AssetType.World => "World",
                CVRAssetInfo.AssetType.Spawnable => "Prop",
                _ => "Unknown"
            };
            return $"{prefix}: {info.gameObject.name}";
        }

        public override void Dispose()
        {
            _cachedContent = null;
        }
    }
}
#endif