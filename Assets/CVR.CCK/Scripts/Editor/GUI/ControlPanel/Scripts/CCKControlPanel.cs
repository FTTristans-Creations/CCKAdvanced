#if UNITY_EDITOR
using System.Collections.Generic;
using System.Threading.Tasks;
using ABI.CCK.Scripts;
using CVR.CCKEditor;
using CVR.CCKEditor.API;
using CVR.CCKEditor.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class CCKControlPanel : EditorWindow 
{
    private static int CurrentTab
    {
        get => SessionState.GetInt("CCK.ControlPanel.SelectedTab", 0);
        set => SessionState.SetInt("CCK.ControlPanel.SelectedTab", value);
    } 
    
    private readonly ICCKTab[] _tabs = {
        new HubTab(),
        new BuilderTab(),
        new BrowserTab(),
        new SettingsTab(),
    };

    private ICCKTab _activeTab;
    private VisualElement _root;
    private VisualElement _tabContainer;
    private VisualElement _contentContainer;
    private Label _headerVersion;
    private DropdownField _languageSelect;

    private VisualElement _btnHub;
    private VisualElement _btnBuilder;
    private VisualElement _btnBrowser;
    private VisualElement _btnSettings;

    [MenuItem(CCKMenuItems.RootMenuName + "/Control Panel", false, 201)]
    private static void ShowWindow() 
    {
        CCKControlPanel window = GetWindow<CCKControlPanel>("CCK Control Panel");
        window.minSize = new Vector2(300, 400);
    }

    private async void CreateGUI()
    {
        Resources.Load<VisualTreeAsset>("CCKControlPanel/UI/ControlPanel").CloneTree(rootVisualElement);
        CCKLocalizationManager.LocalizeVisualTree(rootVisualElement, "ControlPanel");
        
        _contentContainer = rootVisualElement.Q<VisualElement>("Body");
        _headerVersion = rootVisualElement.Q<Label>("HeaderVersion");
        _headerVersion.text = $"CCK Version: {CVRCommon.CCK_VERSION_FULL}";
        
        SetupLocaleDropdown();
        SetupTabButtons();
        LocalizeTabLabels();

        await AttemptAutoAuthentication();
        UpdateTabsRequiringAuth(ApiConnection.AuthState);
        SwitchTab(CurrentTab);

        CCKLocalizationManager.OnLanguageChanged += OnLocaleChanged;
        ApiConnection.OnAuthStateChanged += OnAuthStateChanged;
    }

    private void OnDestroy()
    {
        CCKLocalizationManager.OnLanguageChanged -= OnLocaleChanged;
        ApiConnection.OnAuthStateChanged -= OnAuthStateChanged;
    }

    private static async Task AttemptAutoAuthentication()
    {
        ApiCredentialsHandler.LoadAllProfiles();
        if (ApiCredentialsHandler.IsCurrentCredentialsLoaded() &&
            ApiConnection.AuthState != ApiConnection.AuthenticationState.LoggedIn)
        {
            try
            {
                ApiConnection.Logout();
                if (!await ApiConnection.Login())
                    Debug.LogWarning("[CCK Control Panel] Auto-authentication failed with saved credentials");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CCK Control Panel] Auto-authentication failed: {e.Message}");
                Debug.LogException(e);
            }
        }
    }

    private void SetupLocaleDropdown()
    {
        _languageSelect = rootVisualElement.Q<DropdownField>("LanguageSelect");
        RefreshLocaleDropdown();

        _languageSelect.RegisterValueChangedCallback(evt =>
            CCKLocalizationManager.SetLocaleByIdentifier(evt.newValue));

        _languageSelect.RegisterCallback<PointerDownEvent>(_ => RefreshLocaleDropdown());
    }

    private void RefreshLocaleDropdown()
    {
        var localeInfos = CCKLocalizationManager.GetAllLocaleAssets();
        var localeChoices = new List<string>(localeInfos.Length);
        foreach (var localeInfo in localeInfos)
            localeChoices.Add(localeInfo.identifierName);

        _languageSelect.choices = localeChoices;
        _languageSelect.value = CCKLocalizationManager.CurrentLocale.identifierName;
    }

    private void SetupTabButtons()
    {
        _tabContainer = rootVisualElement.Q<VisualElement>("Tabs");
        _btnHub = _tabContainer.Q<VisualElement>("btn-Hub");
        _btnBuilder = _tabContainer.Q<VisualElement>("btn-Builder");
        _btnBrowser = _tabContainer.Q<VisualElement>("btn-Browser");
        _btnSettings = _tabContainer.Q<VisualElement>("btn-Settings");

        _btnHub.RegisterCallback<ClickEvent>(_ => SwitchTab(0));
        _btnBuilder.RegisterCallback<ClickEvent>(_ => SwitchTab(1));
        _btnBrowser.RegisterCallback<ClickEvent>(_ => SwitchTab(2));
        _btnSettings.RegisterCallback<ClickEvent>(_ => SwitchTab(3));
    }

    private bool TabRequiresAuth(int index) => _tabs[index].RequiresAuthentication;

    private void SwitchTab(int index)
    {
        ICCKTab newTab = _tabs[index];

        if (TabRequiresAuth(index) && !ApiConnection.IsLoggedIn)
        {
            index = 0;
            newTab = _tabs[0];
        }

        CurrentTab = index;

        _activeTab?.DestroyTab();
        _contentContainer.Clear();

        foreach (VisualElement button in _tabContainer.Children())
        {
            Label label = button.Q<Label>();
            bool isSelected = label?.text == newTab.ShortName;
            button.EnableInClassList("cck-tab-selected", isSelected);
        }

        _activeTab = newTab;
        _activeTab.CreateTab(_contentContainer);
    }

    private void LocalizeTabLabels()
    {
        _btnHub.Q<Label>().text = CCKLocalizationManager.GetString("ControlPanel.TAB_HUB");
        _btnBuilder.Q<Label>().text = CCKLocalizationManager.GetString("ControlPanel.TAB_BUILDER");
        _btnBrowser.Q<Label>().text = CCKLocalizationManager.GetString("ControlPanel.TAB_BROWSER");
        _btnSettings.Q<Label>().text = CCKLocalizationManager.GetString("ControlPanel.TAB_SETTINGS");
    }

    private void UpdateTabsRequiringAuth(ApiConnection.AuthenticationState authState)
    {
        bool isLoggedIn = authState == ApiConnection.AuthenticationState.LoggedIn;
        _btnBuilder.EnableInClassList("cck-tab-disabled", !isLoggedIn);
        _btnBrowser.EnableInClassList("cck-tab-disabled", !isLoggedIn);
    }

    private void OnGUI()
    {
        if (Event.current.type == EventType.Layout) _activeTab?.OnLayout();
    }

    private void OnAuthStateChanged(ApiConnection.AuthenticationState newState)
    {
        UpdateTabsRequiringAuth(newState);
        
        // Alert the tabs that we have logged out
        if (newState == ApiConnection.AuthenticationState.NotLoggedIn)
            foreach (ICCKTab tab in _tabs) tab.OnAuthLoggedOut();
    }
    
    private void OnLocaleChanged(CCKLocaleAsset _)
    {
        LocalizeTabLabels();
        SwitchTab(CurrentTab);
    }
}
#endif
