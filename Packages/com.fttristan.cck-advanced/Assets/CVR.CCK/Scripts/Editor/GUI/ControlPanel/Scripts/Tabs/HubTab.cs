#if UNITY_EDITOR
using System.Collections.Generic;
using System.Threading.Tasks;
using CVR.CCK;
using UnityEngine;
using UnityEngine.UIElements;
using CVR.CCKEditor.API;
using CVR.CCKEditor.Localization;

public class HubTab : CCKTabBase
{
    #region Properties
    
    public override string FullName => "Community Hub";
    public override string ShortName => "Hub";
    public override string IconResource => "CCKControlPanel/Icons/HubIcon";
    
    public override bool RequiresAuthentication => false;
    
    #endregion Properties

    #region Fields - UI Elements
    
    // Login
    private VisualElement _loginBox;
    private TextField _usernameInput;
    private TextField _accessKeyInput;
    private Button _loginButton;
    private Label _loginFailedLabel;
    private DropdownField _loginProfileSelector;
    
    // Account Info
    private VisualElement _accountInfoBox;
    private Label _authValueLabel;
    private Label _rankValueLabel;
    private Label _unlockValueLabel;
    private DropdownField _accountProfileSelector;
    
    // Statistics
    private VisualElement _statsBox;
    private Label _publicAvatarsValue;
    private Label _publicWorldsValue;
    private Label _publicSpawnablesValue;
    private Label _privateAvatarsValue;
    private Label _privateWorldsValue;
    private Label _privateSpawnablesValue;
    private Label _totalAvatarsValue;
    private Label _totalWorldsValue;
    private Label _totalSpawnablesValue;
    
    // Warnings
    private HelpBox _limitAvatarsWorldsBox;
    private HelpBox _limitSpawnablesBox;
    private HelpBox _limitInfoBox;
    
    #endregion Fields - UI Elements

    #region Lifecycle Methods
    
    protected override void OnCreateTab()
    {
        Resources.Load<VisualTreeAsset>("CCKControlPanel/UI/CommunityHub").CloneTree(TabContainer);
        CCKLocalizationManager.LocalizeVisualTree(TabContainer, "CommunityHub");
        
        // Note: Credentials are loaded by the control panel now
        
        CacheUIElements();
        SetupEventHandlers();
        
        // Populate UI with current credentials if available
        PopulateCredentialsFields();
        UpdateUIState();
    }

    protected override void OnDestroyTab()
    {
        // No special cleanup needed
    }

    protected override void HandleLayout()
    {
        // No special layout handling needed
    }
    
    #endregion Lifecycle Methods

    #region UI Setup Methods
    
    private void CacheUIElements()
    {
        CacheLoginElements();
        CacheAccountElements();
        CacheStatisticsElements();
        CacheWarningElements();
    }
    
    private void CacheLoginElements()
    {
        _loginBox = TabContainer.Q("cck-login-box");
        _usernameInput = TabContainer.Q<TextField>("cck-username-input");
        _accessKeyInput = TabContainer.Q<TextField>("cck-access-key-input");
        _loginButton = TabContainer.Q<Button>("cck-login-button");
        _loginFailedLabel = TabContainer.Q<Label>("cck-login-failed-label");
        _loginProfileSelector = _loginBox.Q<DropdownField>("ProfileSelector");
    }
    
    private void CacheAccountElements()
    {
        _accountInfoBox = TabContainer.Q("cck-account-info-box");
        _authValueLabel = TabContainer.Q<Label>("cck-auth-value");
        _rankValueLabel = TabContainer.Q<Label>("cck-rank-value");
        _unlockValueLabel = TabContainer.Q<Label>("cck-unlock-value");
        _accountProfileSelector = _accountInfoBox.Q<DropdownField>("ProfileSelector");
    }
    
    private void CacheStatisticsElements()
    {
        _statsBox = TabContainer.Q("cck-stats-box");
        _publicAvatarsValue = TabContainer.Q<Label>("cck-public-avatars-value");
        _publicWorldsValue = TabContainer.Q<Label>("cck-public-worlds-value");
        _publicSpawnablesValue = TabContainer.Q<Label>("cck-public-spawnables-value");
        _privateAvatarsValue = TabContainer.Q<Label>("cck-private-avatars-value");
        _privateWorldsValue = TabContainer.Q<Label>("cck-private-worlds-value");
        _privateSpawnablesValue = TabContainer.Q<Label>("cck-private-spawnables-value");
        _totalAvatarsValue = TabContainer.Q<Label>("cck-total-avatars-value");
        _totalWorldsValue = TabContainer.Q<Label>("cck-total-worlds-value");
        _totalSpawnablesValue = TabContainer.Q<Label>("cck-total-spawnables-value");
    }
    
    private void CacheWarningElements()
    {
        _limitAvatarsWorldsBox = TabContainer.Q<HelpBox>("cck-limit-avatars-worlds");
        _limitSpawnablesBox = TabContainer.Q<HelpBox>("cck-limit-spawnables");
        _limitInfoBox = TabContainer.Q<HelpBox>("cck-limit-info");
    }

    private void SetupEventHandlers()
    {
        SetupLoginHandlers();
        SetupProfileHandlers();
        SetupExternalLinkHandlers();
    }
    
    private void SetupLoginHandlers()
    {
        _loginButton.clicked += OnLoginClicked;
        TabContainer.Q<Button>("cck-logout-button").clicked += OnLogoutClicked;
    }
    
    private void SetupProfileHandlers()
    {
        _loginProfileSelector.RegisterValueChangedCallback(_ => OnProfileChanged(_loginProfileSelector.index));
        _accountProfileSelector.RegisterValueChangedCallback(_ => OnProfileChanged(_accountProfileSelector.index));
    }
    
    private void SetupExternalLinkHandlers()
    {
        TabContainer.Q<Button>("cck-create-account-button").clicked += () => Application.OpenURL(WebLinks.CVRHubSignupUrl);
        TabContainer.Q<Button>("cck-open-hub-button").clicked += () => Application.OpenURL(WebLinks.CVRHubUrl);
        TabContainer.Q<Button>("cck-open-feedback-button").clicked += () => Application.OpenURL(WebLinks.CVRFeedbackUrl);
        TabContainer.Q<Button>("cck-open-docs-button").clicked += () => Application.OpenURL(WebLinks.CCKDocsUrl);
    }
    
    #endregion UI Setup Methods

    #region UI Update Methods
    
    private void UpdateUIState()
    {
        bool isLoggedIn = ApiConnection.AuthState == ApiConnection.AuthenticationState.LoggedIn;
        
        UpdateDisplayState(isLoggedIn);
        UpdateProfileSelectors();
        
        if (isLoggedIn)
        {
            UpdateAccountInfo();
            UpdateContentStats();
            UpdateWarnings();
        }
    }
    
    private void UpdateDisplayState(bool isLoggedIn)
    {
        _loginBox.style.display = isLoggedIn ? DisplayStyle.None : DisplayStyle.Flex;
        _accountInfoBox.style.display = isLoggedIn ? DisplayStyle.Flex : DisplayStyle.None;
        _statsBox.style.display = isLoggedIn ? DisplayStyle.Flex : DisplayStyle.None;
    }
    
    private void UpdateProfileSelectors()
    {
        _loginProfileSelector.index = ApiCredentialsHandler.CurrentProfile;
        _accountProfileSelector.index = ApiCredentialsHandler.CurrentProfile;

        var choices = BuildProfileChoices();
        _loginProfileSelector.choices = choices;
        _accountProfileSelector.choices = choices;
        
        string displayValue = $"Profile {ApiCredentialsHandler.CurrentProfile + 1}";
        _loginProfileSelector.SetValueWithoutNotify(displayValue);
        _accountProfileSelector.SetValueWithoutNotify(displayValue);

        return;
        List<string> BuildProfileChoices()
        {
            var profileChoices = new List<string>();
            for (int i = 0; i < 8; i++)
            {
                if (ApiCredentialsHandler.IsCredentialsLoaded(i))
                {
                    var (username, _) = ApiCredentialsHandler.GetCredentials(i);
                    profileChoices.Add($"{i + 1}. {username}");
                }
                else
                {
                    profileChoices.Add($"{i + 1}. Empty");
                }
            }
            return profileChoices;
        }
    }
    
    private void UpdateAccountInfo()
    {
        _authValueLabel.text = ApiConnection.UserInfo.Name;
        _rankValueLabel.text = ApiConnection.UserInfo.UserRank;
        _unlockValueLabel.text = ApiConnection.UserInfo.IsAccountUnlocked ? "Unlocked" : "Locked";
    }
    
    private void UpdateContentStats()
    {
        UpdatePublicStats();
        UpdateTotalStats();
        UpdatePrivateStats();
    }
    
    private void UpdatePublicStats()
    {
        _publicAvatarsValue.text = ApiConnection.PublicContentStats.Avatars.ToString();
        _publicWorldsValue.text = ApiConnection.PublicContentStats.Worlds.ToString();
        _publicSpawnablesValue.text = ApiConnection.PublicContentStats.Spawnables.ToString();
    }
    
    private void UpdateTotalStats()
    {
        _totalAvatarsValue.text = ApiConnection.ContentStats.Avatars.ToString();
        _totalWorldsValue.text = ApiConnection.ContentStats.Worlds.ToString();
        _totalSpawnablesValue.text = ApiConnection.ContentStats.Spawnables.ToString();
    }
    
    private void UpdatePrivateStats()
    {
        int privateAvatars = ApiConnection.ContentStats.Avatars - ApiConnection.PublicContentStats.Avatars;
        int privateWorlds = ApiConnection.ContentStats.Worlds - ApiConnection.PublicContentStats.Worlds;
        int privateSpawnables = ApiConnection.ContentStats.Spawnables - ApiConnection.PublicContentStats.Spawnables;
        
        _privateAvatarsValue.text = privateAvatars.ToString();
        _privateWorldsValue.text = privateWorlds.ToString();
        _privateSpawnablesValue.text = privateSpawnables.ToString();
    }
    
    private void UpdateWarnings()
    {
        bool nearingLimitAvatarsWorlds = ApiConnection.IsNearAvatarsAndWorldsLimit;
        bool nearingLimitSpawnables = ApiConnection.IsNearSpawnablesLimit;
        
        UpdateWarningVisibility(nearingLimitAvatarsWorlds, nearingLimitSpawnables);
        
        if (nearingLimitAvatarsWorlds) UpdateAvatarsWorldsWarning();
        if (nearingLimitSpawnables) UpdateSpawnablesWarning();
    }
    
    private void UpdateWarningVisibility(bool showAvatarsWorlds, bool showSpawnables)
    {
        _limitAvatarsWorldsBox.style.display = showAvatarsWorlds ? DisplayStyle.Flex : DisplayStyle.None;
        _limitSpawnablesBox.style.display = showSpawnables ? DisplayStyle.Flex : DisplayStyle.None;
        _limitInfoBox.style.display = (showAvatarsWorlds || showSpawnables) ? DisplayStyle.Flex : DisplayStyle.None;
        _limitInfoBox.text = CCKLocalizationManager.GetString("CommunityHub.LIMIT_WARNING_INFO");
    }
    
    private void UpdateAvatarsWorldsWarning()
    {
        int privateTotal = int.Parse(_privateAvatarsValue.text) + int.Parse(_privateWorldsValue.text);
        string warningFormat = CCKLocalizationManager.GetString("CommunityHub.LIMIT_WARNING_AVATARS_WORLDS");
        _limitAvatarsWorldsBox.text = string.Format(warningFormat, privateTotal, ApiConnection.ContentLimits.AvatarsAndWorlds);
    }
    
    private void UpdateSpawnablesWarning()
    {
        int privateSpawnables = int.Parse(_privateSpawnablesValue.text);
        string warningFormat = CCKLocalizationManager.GetString("CommunityHub.LIMIT_WARNING_SPAWNABLES");
        _limitSpawnablesBox.text = string.Format(warningFormat, privateSpawnables, ApiConnection.ContentLimits.Spawnables);
    }
    
    #endregion UI Update Methods

    #region Event Handlers
    
    private void OnLoginClicked()
    {
        _loginFailedLabel.style.display = DisplayStyle.None;
        
        string username = _usernameInput.value;
        string accessKey = _accessKeyInput.value.Trim();
        
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(accessKey))
        {
            ShowLoginFailed("CommunityHub.LOGIN_FAILED_NO_CREDENTIALS");
            return;
        }
        
        _ = AttemptLogin(username, accessKey);
    }
    
    private void OnLogoutClicked()
    {
        ApiConnection.Logout();
        ApiCredentialsHandler.ClearCredentials();
        UpdateUIState();
    }
    
    private void OnProfileChanged(int selectedProfile)
    {
        if (selectedProfile == ApiCredentialsHandler.CurrentProfile
            || selectedProfile < 0)
            return;

        ApiCredentialsHandler.SwitchProfile(selectedProfile);
    
        PopulateCredentialsFields();
        
        ApiConnection.Logout();
        if (ApiCredentialsHandler.IsCurrentCredentialsLoaded())
            _ = AttemptLogin(ApiCredentialsHandler.Username, ApiCredentialsHandler.AccessKey);
    
        UpdateUIState();
    }
    
    #endregion Event Handlers

    #region Helper Methods
    
    private void PopulateCredentialsFields()
    {
        if (ApiCredentialsHandler.IsCurrentCredentialsLoaded())
        {
            _usernameInput.value = ApiCredentialsHandler.Username;
            _accessKeyInput.value = ApiCredentialsHandler.AccessKey;
        }
        else
        {
            _usernameInput.value = string.Empty;
            _accessKeyInput.value = string.Empty;
        }
    }
    
    private void ShowLoginFailed(string localeId)
    {
        _loginFailedLabel.text = $"{CCKLocalizationManager.GetString("CommunityHub.LOGIN_FAILED_GENERIC")}. {CCKLocalizationManager.GetString(localeId)}";
        _loginFailedLabel.style.display = DisplayStyle.Flex;
    }
    
    private async Task AttemptLogin(string username, string accessKey)
    {
        _loginButton.text = CCKLocalizationManager.GetString("CommunityHub.LOGIN_BUTTON_IN_PROGRESS");
        _loginFailedLabel.style.display = DisplayStyle.None;
        
        try
        {
            ApiConnection.Logout();
            ApiCredentialsHandler.SaveCredentials(username, accessKey);
            await ApiConnection.Login();
            
            if (ApiConnection.AuthState == ApiConnection.AuthenticationState.LoggedIn)
            {
                UpdateUIState();
            }
            else
            {
                Debug.LogWarning("Authentication failed. Unable to proceed.");
                ShowLoginFailed("CommunityHub.LOGIN_FAILED_GENERIC");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ABI:CCK] Unable to authenticate using provided credentials.");
            Debug.LogException(e);
            ShowLoginFailed("CommunityHub.LOGIN_FAILED_INVALID_CREDENTIALS");
        }
        
        _loginButton.text = CCKLocalizationManager.GetString("CommunityHub.LOGIN_BUTTON_TEXT");
    }
    
    #endregion Helper Methods
}
#endif