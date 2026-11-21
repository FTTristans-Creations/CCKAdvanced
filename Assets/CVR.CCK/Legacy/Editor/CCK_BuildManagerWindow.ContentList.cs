/*using System.Collections.Generic;
using System.Linq;
using ABI.CCK.Components;
using CVR.CCKEditor.API;
using UnityEditor;
using UnityEngine;

namespace ABI.CCK.Scripts.Editor
{
    public partial class CCK_BuildManagerWindow
    {
        private const string SessionKeySelectedContentIndex = "CCK_SelectedContentIndex";
        private static int SelectedContentIndex
        {
            get => SessionState.GetInt(SessionKeySelectedContentIndex, 0);
            set => SessionState.SetInt(SessionKeySelectedContentIndex, Mathf.Max(0, value));
        }
        
        private readonly List<ContentItem> _availableContent = new();

        private class ContentItem
        {
            public string DisplayName { get; set; }
            public GameObject GameObject { get; set; }
            public ContentType Type { get; set; }
        }

        private enum ContentType
        {
            Avatar,
            Spawnable,
            World
        }
        
        private void Tab_FoundContent()
        {
            if (!ApiConnection.IsLoggedIn)
            {
                EditorGUILayout.HelpBox("Please log in through the Community Hub tab to upload content.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_HEADING_FOUNDCONTENT"), EditorStyles.boldLabel);

            if (!CVRCommon.IsUnityVersionSupported())
            {
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_WORLDS_ERROR_UNITY_UNSUPPORTED"), MessageType.Error);
                return;
            }
            
            EditorGUILayout.Space();
            
#if PLATFORM_ANDROID
            if (!ABI.CCK.Dependencies.OpenXR.OpenXR_Configurator.IsImported())
            {
                EditorGUILayout.HelpBox("OpenXR needs to be imported prior to uploading Android content!", MessageType.Error);
                if (GUILayout.Button("Import OpenXR")) ABI.CCK.Dependencies.OpenXR.OpenXR_Configurator.RunConfiguration();
                return;
            }
            if (!ABI.CCK.Dependencies.OpenXR.OpenXR_Configurator.IsConfigured())
            {
                EditorGUILayout.HelpBox("OpenXR needs to be configured prior to uploading Android content!", MessageType.Error);
                if (GUILayout.Button("Configure OpenXR")) ABI.CCK.Dependencies.OpenXR.OpenXR_Configurator.RunConfiguration();
                return;
            }
#endif

            RefreshContentList();

            if (_availableContent.Count == 0)
            {
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_WORLDS_ERROR_NO_CONTENT"), MessageType.Info);
                return;
            }

            if (HasMultipleWorlds())
            {
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_WORLDS_ERROR_MULTIPLE_CVR_WORLD"), MessageType.Error);
                return;
            }

            if (HasMixedContent())
            {
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_WORLDS_ERROR_WORLD_CONTAINS_AVATAR"), MessageType.Error);
                return;
            }

            if (HasSingleWorld())
            {
                DisplayContentUi(_availableContent[0]);
            }
            else
            {
                DisplayContentDropdown();
                EditorGUILayout.Space();
                
                if (SelectedContentIndex >= 0 && SelectedContentIndex < _availableContent.Count)
                {
                    ContentItem selectedContent = _availableContent[SelectedContentIndex];
                    DisplayContentUi(selectedContent);
                }
            }
        }

        #region Content List
        
        private void RefreshContentList()
        {
            _availableContent.Clear();

            foreach (CVRWorld world in Resources.FindObjectsOfTypeAll<CVRWorld>())
            {
                if (world.gameObject.activeInHierarchy)
                {
                    _availableContent.Add(new ContentItem
                    {
                        DisplayName = $"World: {world.gameObject.name}",
                        GameObject = world.gameObject,
                        Type = ContentType.World
                    });
                }
            }

            foreach (CVRAvatar avatar in Resources.FindObjectsOfTypeAll<CVRAvatar>())
            {
                if (avatar.gameObject.activeInHierarchy)
                {
                    _availableContent.Add(new ContentItem
                    {
                        DisplayName = $"Avatar: {avatar.gameObject.name}",
                        GameObject = avatar.gameObject,
                        Type = ContentType.Avatar
                    });
                }
            }

            foreach (CVRSpawnable spawnable in Resources.FindObjectsOfTypeAll<CVRSpawnable>())
            {
                if (spawnable.gameObject.activeInHierarchy)
                {
                    _availableContent.Add(new ContentItem
                    {
                        DisplayName = $"Spawnable: {spawnable.gameObject.name}",
                        GameObject = spawnable.gameObject,
                        Type = ContentType.Spawnable
                    });
                }
            }

            if (SelectedContentIndex >= _availableContent.Count)
            {
                SelectedContentIndex = 0;
            }
        }
        
        private bool HasSingleWorld() 
            => _availableContent.Count == 1 && _availableContent[0].Type == ContentType.World;

        private bool HasMultipleWorlds() 
            => _availableContent.Count(c => c.Type == ContentType.World) > 1;

        private bool HasMixedContent()
        {
            bool hasWorld = _availableContent.Any(c => c.Type == ContentType.World);
            bool hasOther = _availableContent.Any(c => c.Type != ContentType.World);
            return hasWorld && hasOther;
        }
        
        private void DisplayContentDropdown()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Select Content:", GUILayout.Width(100));
            
            string[] contentNames = _availableContent.Select(c => c.DisplayName).ToArray();
            SelectedContentIndex = EditorGUILayout.Popup(SelectedContentIndex, contentNames);
            
            EditorGUILayout.EndHorizontal();
        }
        
        #endregion Content List
        
        #region Content Validation

        private void DisplayContentUi(ContentItem content)
        {
            // Content header & ping
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{content.DisplayName}", EditorStyles.boldLabel);
            if (GUILayout.Button("Ping", GUILayout.Width(50))) EditorGUIUtility.PingObject(content.GameObject);
            EditorGUILayout.EndHorizontal();
            
            // Display guid if available
            Rect rect = EditorGUILayout.GetControlRect();
            CVRAssetInfo assetInfo = content.GameObject.GetComponent<CVRAssetInfo>();
            if (assetInfo == null || string.IsNullOrEmpty(assetInfo.objectId))
            {
                EditorGUI.HelpBox(rect, "This content has not yet been uploaded.", MessageType.Info);
            }
            else
            {
                const float dropdownButtonWidth = 20f;

                Rect helpBoxRect = new(rect.x, rect.y, rect.width - dropdownButtonWidth, rect.height);
                EditorGUI.HelpBox(helpBoxRect, $"GUID: {assetInfo.objectId}", MessageType.Info);

                Rect dropdownRect = new(rect.x + rect.width - dropdownButtonWidth, rect.y, dropdownButtonWidth, rect.height);
                if (EditorGUI.DropdownButton(dropdownRect, GUIContent.none, FocusType.Passive)) 
                {
                    GenericMenu menu = new();
                    menu.AddItem(new GUIContent("Copy to Clipboard"), false, () => EditorGUIUtility.systemCopyBuffer = assetInfo.objectId);
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Open in Browser"), false, () => Application.OpenURL(GetHubUrlForContent(assetInfo)));
                    menu.AddItem(new GUIContent("Open in ChilloutVR"), false, () => Application.OpenURL(GetClientUrlForContent(assetInfo)));
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Clear GUID"), false, () =>
                    {
                        if (!EditorUtility.DisplayDialog
                            (
                                "CCK :: Clear Attached GUID", 
                                "Are you sure you want to clear the attached GUID?", 
                                "Yes", "No"
                            )) 
                            return;
                        
                        // Record undo action
                        Undo.RecordObject(assetInfo, "Clear GUID");
                        assetInfo.objectId = string.Empty;
                        
                        RefreshContentList();
                    });
                    menu.ShowAsContext();
                }
            }

            EditorGUILayout.Space();
            
            // Validation label
            EditorGUILayout.LabelField("Validations:", EditorStyles.boldLabel);
            
            switch (content.Type)
            {
                case ContentType.Avatar:
                    CVRAvatar avatar = content.GameObject.GetComponent<CVRAvatar>();
                    OnGUIAvatar(avatar);
                    break;
                case ContentType.Spawnable:
                    CVRSpawnable spawnable = content.GameObject.GetComponent<CVRSpawnable>();
                    OnGUISpawnable(spawnable);
                    break;
                case ContentType.World:
                    DisplayWorldValidation(content);
                    break;
            }
        }
        
        #endregion Content Validation

        #region Fun Stuff
        
        private static string GetHubUrlForContent(CVRAssetInfo assetInfo)
        {
            if (assetInfo == null || string.IsNullOrEmpty(assetInfo.objectId)) return string.Empty;
            return $"https://hub.abinteractive.net/my{assetInfo.type.ToString().ToLower()}s/edit?id={assetInfo.objectId}";
        }
        
        private static string GetClientUrlForContent(CVRAssetInfo assetInfo)
        {
            if (assetInfo == null || string.IsNullOrEmpty(assetInfo.objectId)) return string.Empty;
            string contentType = assetInfo.type == CVRAssetInfo.AssetType.Spawnable ? "prop" : assetInfo.type.ToString().ToLower();
            return $"chilloutvr://details/{contentType}?id={assetInfo.objectId}";
        }

        #endregion Fun Stuff
    }
}*/