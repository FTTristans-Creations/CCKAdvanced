using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CVR.CCKEditor.API;
using CVR.CCKEditor.Localization;
using CVR.CCKEditor.Util;
using CCK.CE.Hacks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ContentTypes = CVR.CCKEditor.API.ContentTypes;
using CVRApiAvatar = CVR.CCKEditor.API.CVRApiAvatar;
using CVRApiSpawnable = CVR.CCKEditor.API.CVRApiSpawnable;
using CVRApiWorld = CVR.CCKEditor.API.CVRApiWorld;
using Debug = UnityEngine.Debug;

public class BrowserTab : CCKTabBase
{
    #region Tab Base

    public override string FullName => "Browser Tab";
    public override string ShortName => "Browser";
    public override string IconResource => "CCKControlPanel/Icons/content-browser";
    public override bool RequiresAuthentication => true;
    
    #endregion Tab Base
    
    #region Search Enums
    
    private enum SortOrder { Ascending, Descending }
    private enum SortBy { Age, Name }
    
    #endregion Search Enums
    
    #region Fields - UI Elements

    private VisualTreeAsset _contentItemTemplate;
    private TextField _searchField;
    private DropdownField _sortOrderDropdown, _sortByDropdown;
    private VisualElement _avatarButton, _propButton, _worldButton;
    private Label _searchFilterResultCounter, _bodyFallbackLabel;
    private ScrollView _scrollView;
    
    #endregion Fields - UI Elements

    #region State
    
    private const string KeyBrowserState = "CCK.Browser";
    
    private ContentTypes CurrentContentType
    {
        get => (ContentTypes)SessionState.GetInt(KeyBrowserState + nameof(CurrentContentType), (int)ContentTypes.Invalid);
        set => SessionState.SetInt(KeyBrowserState + nameof(CurrentContentType), (int)value);
    }
    
    private SortOrder CurrentSortOrder // Noachi said best to show most recent first
    {
        get => (SortOrder)SessionState.GetInt(KeyBrowserState + nameof(CurrentSortOrder), (int)SortOrder.Ascending);
        set => SessionState.SetInt(KeyBrowserState + nameof(CurrentSortOrder), (int)value);
    }

    private SortBy CurrentSortBy // Noachi said best to show most recent first
    {
        get => (SortBy)SessionState.GetInt(KeyBrowserState + nameof(CurrentSortBy), (int)SortBy.Age);
        set => SessionState.SetInt(KeyBrowserState + nameof(CurrentSortBy), (int)value);
    }

    private string CurrentSearchTerm
    {
        get => SessionState.GetString(KeyBrowserState + nameof(CurrentSearchTerm), string.Empty);
        set => SessionState.SetString(KeyBrowserState + nameof(CurrentSearchTerm), value);
    }
    
    private bool _isLoading;

    private readonly Dictionary<ContentTypes, List<(CVRApiContent, VisualElement)>> _cachedContent = new();
    private readonly List<VisualElement> _displayedElements = new();
    private readonly Dictionary<string, Texture2D> _imageCache = new();
    
    #endregion State
    
    protected override void OnCreateTab()
    {
        Resources.Load<VisualTreeAsset>("CCKControlPanel/UI/ContentBrowser").CloneTree(TabContainer);
        CCKLocalizationManager.LocalizeVisualTree(TabContainer, "BrowserTab");

        // Setup UXML
        _contentItemTemplate = Resources.Load<VisualTreeAsset>("CCKControlPanel/UI/Elements/ContentEntry");
        _searchField = TabContainer.Q<TextField>("SearchField");
        _sortOrderDropdown = TabContainer.Q<DropdownField>("SortOrderDropdown");
        _sortByDropdown = TabContainer.Q<DropdownField>("SortByDropdown");
        _avatarButton = TabContainer.Q<VisualElement>("btn-avatars");
        _propButton = TabContainer.Q<VisualElement>("btn-props");
        _worldButton = TabContainer.Q<VisualElement>("btn-worlds");
        _searchFilterResultCounter = TabContainer.Q<Label>("SearchResultsLabel");
        _bodyFallbackLabel = TabContainer.Q<Label>("BodyFallbackLabel");
        _scrollView = TabContainer.Q<ScrollView>("Body");
        
        // Setup search dropdowns
        _sortOrderDropdown.choices = new List<string> { "Ascending", "Descending" };
        _sortByDropdown.choices = new List<string> { "Age", "Name" };
        
        // Setup bindings
        _searchField.RegisterValueChangedCallback(evt => { CurrentSearchTerm = evt.newValue ?? string.Empty; RefreshDisplay(); });
        _sortOrderDropdown.RegisterValueChangedCallback(_ => { CurrentSortOrder = (SortOrder)_sortOrderDropdown.index; OnSortChanged(); });
        _sortByDropdown.RegisterValueChangedCallback(_ => { CurrentSortBy = (SortBy)_sortByDropdown.index; OnSortChanged(); });
        _avatarButton.RegisterCallback<ClickEvent>(_ => SwitchContentType(ContentTypes.Avatar));
        _propButton.RegisterCallback<ClickEvent>(_ => SwitchContentType(ContentTypes.Spawnable));
        _worldButton.RegisterCallback<ClickEvent>(_ => SwitchContentType(ContentTypes.World));
        
        // Set session state
        _sortOrderDropdown.SetValueWithoutNotify((int)CurrentSortOrder);
        _sortByDropdown.SetValueWithoutNotify((int)CurrentSortBy);
        _searchField.SetValueWithoutNotify(CurrentSearchTerm);

        // If invalid, default to Avatar, otherwise restore last used content type
        SwitchContentType(CurrentContentType == ContentTypes.Invalid 
            ? ContentTypes.Avatar : CurrentContentType, true);
    }

    protected override void OnDestroyTab()
    {
        ClearDisplay();

        // Destroy textures in memory because making a mess is bad >:(
        // TODO: We could visual element schedule for 10s or so, so reloading tab does not fetch from disk.
        foreach (var tex in _imageCache.Values) if (tex) UnityEngine.Object.DestroyImmediate(tex);
        
        _imageCache.Clear();
        _cachedContent.Clear();
    }

    protected override void HandleLayout() { }

    public override void OnAuthLoggedOut()
    {
        // Reset state when logged out
        CurrentContentType = ContentTypes.Invalid;
        CurrentSortOrder = SortOrder.Ascending;
        CurrentSortBy = SortBy.Age;
        CurrentSearchTerm = string.Empty;
    }
    
    private async void SwitchContentType(ContentTypes contentType, bool force = false)
    {
        if (!force && (_isLoading || CurrentContentType == contentType))
            return;
        
        CurrentContentType = contentType;
        UpdateButtonStates();
        
        if (!force && _cachedContent.ContainsKey(contentType))
            RefreshDisplay();
        else
            await LoadContent();
    }

    private async void OnSortChanged()
    {
        _cachedContent.Remove(CurrentContentType);
        await LoadContent();
    }

    private void UpdateButtonStates()
    {
        _avatarButton.EnableInClassList("cck-tab-selected", CurrentContentType == ContentTypes.Avatar);
        _propButton.EnableInClassList("cck-tab-selected", CurrentContentType == ContentTypes.Spawnable);
        _worldButton.EnableInClassList("cck-tab-selected", CurrentContentType == ContentTypes.World);
    }

    private async Task LoadContent()
    {
        if (_isLoading) return;
        _isLoading = true;
        
        ClearDisplay();
        ShowFallback("Fetching Content...");

        try
        {
            string orderBy = CurrentSortBy.ToString();
            bool ascending = CurrentSortOrder == SortOrder.Ascending;

            var contentList = CurrentContentType switch
            {
                ContentTypes.Avatar => await CVRApiContent.GetAllMyContentUnpaged<CVRApiAvatar>(ascending, orderBy),
                ContentTypes.World => await CVRApiContent.GetAllMyContentUnpaged<CVRApiWorld>(ascending, orderBy),
                ContentTypes.Spawnable => await CVRApiContent.GetAllMyContentUnpaged<CVRApiSpawnable>(ascending, orderBy),
                _ => null
            };

            var allContent = new List<(CVRApiContent, VisualElement)>();
            if (contentList is { Count: > 0 })
            {
                // Reversed I think is correct. Swear the endpoint returns inverted.
                // For display purposes, the first is newest by default, and oldest is at bottom.
                for (int i = contentList.Count - 1; i >= 0; i--)
                {
                    var item = contentList[i];
                    if (!item.IsValid) continue;
                    allContent.Add((item, CreateContentElement(item, i)));
                }
            }
            
            _cachedContent[CurrentContentType] = allContent;

            RefreshDisplay();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            ShowFallback("Error loading content");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void RefreshDisplay()
    {
        ClearDisplay();

        if (!_cachedContent.TryGetValue(CurrentContentType, out var content) || content.Count == 0)
        {
            ShowFallback("No Content :(");
            return;
        }

        int displayed = 0;
        foreach (var (item, element) in content)
        {
            if (!string.IsNullOrEmpty(CurrentSearchTerm) &&
                !item.Name.Contains(CurrentSearchTerm, StringComparison.OrdinalIgnoreCase) &&
                !item.Description.Contains(CurrentSearchTerm, StringComparison.OrdinalIgnoreCase))
            {
                element.style.display = DisplayStyle.None;
                continue;
            }

            element.style.display = DisplayStyle.Flex;
            _scrollView.Add(element);
            _displayedElements.Add(element);
            displayed++;
        }

        if (displayed == 0)
        {
            ShowFallback("No results found >:(");
            return;
        }

        SetResultsLabel(displayed != content.Count, $"Showing {displayed} of {content.Count} Items");
    }

    private VisualElement CreateContentElement(CVRApiContent data, int index)
    {
        var element = _contentItemTemplate.CloneTree();

        element.Q<Label>("label-name").text = data.Name;
        element.Q<Label>("label-description").text = data.Description;
        element.Q<Label>("label-status").text = data.IsPublic ? "Public" : "Private";
        element.Q<Label>("label-platform").text = string.Join(", ", data.PlatformReadable);

        element.Q<Button>("button-view-on-hub").clicked += () => Application.OpenURL(data.HubUrl);
        element.Q<Button>("button-view-in-client").clicked += () => Application.OpenURL(data.ClientUrl);

        var copyButton = element.Q<Button>("button-copy-asset-id");
        copyButton.clicked += () =>
        {
            EditorGUIUtility.systemCopyBuffer = data.ID;
            copyButton.text = "Copied!";
            copyButton.schedule.Execute(() => copyButton.text = "Copy Asset ID").StartingIn(1000);
        };

        var isPro = EditorGUIUtility.isProSkin; // Dark Mode :3
        var bgColor = (index + 1) % 2 == 0 ? (isPro ? new Color32(56, 56, 56, 255) : new Color32(238, 238, 238, 255)) :
                                              (isPro ? new Color32(66, 66, 66, 255) : new Color32(246, 246, 246, 255));
        element.Q<VisualElement>("ContentEntry").style.backgroundColor = new StyleColor(bgColor);

        var image = element.Q<Image>("image-thumbnail");
        image.scaleMode = ScaleMode.ScaleToFit;
        
        // Check if we already loaded texture into memory
        if (_imageCache.TryGetValue(data.ID, out var cachedTex))
        {
            image.image = cachedTex;
        }
        else
        {
            // Download image or load from cache on disk
            ImageDownloader.GetImage(data.ID, data.ContentTypeString.ToLower(), data.ImageUrlString, tex =>
            {
                if (tex)
                {
                    _imageCache[data.ID] = tex;
                    image.image = tex;
                }
                else
                {
                    image.image = EditorGUIUtility.IconContent("console.warnicon").image;
                }
            });
        }

        return element;
    }

    private void ClearDisplay()
    {
        foreach (var element in _displayedElements) element?.RemoveFromHierarchy();
        _displayedElements.Clear();
        _bodyFallbackLabel.style.display = DisplayStyle.None;
    }

    private void SetResultsLabel(bool enabled, string message = null)
    {
        _searchFilterResultCounter.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
        _searchFilterResultCounter.text = message;
    }

    private void ShowFallback(string message)
    {
        _bodyFallbackLabel.text = message;
        _bodyFallbackLabel.style.display = DisplayStyle.Flex;
        SetResultsLabel(false);
    }
}