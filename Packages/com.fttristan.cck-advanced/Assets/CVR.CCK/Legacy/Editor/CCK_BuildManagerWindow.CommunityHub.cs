/*using CVR.CCKEditor.API;
using UnityEditor;
using UnityEngine;

namespace ABI.CCK.Scripts.Editor
{
    public partial class CCK_BuildManagerWindow
    {
        private const string DocumentationUrl = "docs.chilloutvr.net/";
        private const string FeedbackUrl = "https://github.com/Alpha-Blend-Interactive/ChilloutVR";
        
        private void Tab_LogIn()
        {
            EditorGUILayout.LabelField("ABI Community Hub Access", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_INFOTEXT_SIGNIN1"));
            EditorGUILayout.LabelField(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_INFOTEXT_SIGNIN2"));
            EditorGUILayout.LabelField(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_INFOTEXT_SIGNIN3"));
            EditorGUILayout.Space();
            _inputUsername = EditorGUILayout.TextField(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_LOGIN_TEXT_USERNAME"), _inputUsername);
            _inputAccessKey = EditorGUILayout.PasswordField(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_LOGIN_TEXT_ACCESSKEY"), _inputAccessKey);

            if (GUILayout.Button(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_LOGIN_BUTTON")))
            {
                _ = AttemptLoginWithCredentials();
            }

            if (ApiConnection.AuthState == ApiConnection.AuthenticationState.LoginFailed && !ApiConnection.IsLoggedIn)
            {
                GUILayout.Label(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_LOGIN_CREDENTIALS_INCORRECT"));
            }
        }

        private void Tab_CommunityHub()
        {
            EditorGUILayout.LabelField(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_HEADING_ACCOUNT_INFO"), EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_INFOTEXT_AUTHENTICATED_AS"), ApiConnection.UserInfo.Name);
            EditorGUILayout.LabelField(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_INFOTEXT_USER_RANK"), ApiConnection.UserInfo.UserRank);
            EditorGUILayout.LabelField("Account Unlock", $"{(ApiConnection.UserInfo.IsAccountUnlocked ? "Unlocked" : "Locked")}");
            EditorGUILayout.Space();
            
            bool nearingLimitAvatarsAndWorlds = ApiConnection.IsNearAvatarsAndWorldsLimit;
            bool nearingLimitSpawnables = ApiConnection.IsNearSpawnablesLimit;
            
            if (nearingLimitAvatarsAndWorlds)
            {
                int privateAvatars = ApiConnection.ContentStats.Avatars - ApiConnection.PublicContentStats.Avatars;
                int privateWorlds = ApiConnection.ContentStats.Worlds - ApiConnection.PublicContentStats.Worlds;
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_ACCOUNT_UPLOAD_LIMIT_AVATAR_WORLD_WARNING")
                    .Replace("{x}", $"{privateAvatars + privateWorlds}")
                    .Replace("{y}", ApiConnection.ContentLimits.AvatarsAndWorlds.ToString()), MessageType.Warning);
            }
            
            if (nearingLimitSpawnables)
            {
                int privateSpawnables = ApiConnection.ContentStats.Spawnables - ApiConnection.PublicContentStats.Spawnables;
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_ACCOUNT_UPLOAD_LIMIT_SPAWNABLE_WARNING")
                    .Replace("{x}", $"{privateSpawnables}")
                    .Replace("{y}", ApiConnection.ContentLimits.Spawnables.ToString()), MessageType.Warning);
            }
            
            if (nearingLimitAvatarsAndWorlds || nearingLimitSpawnables) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_ACCOUNT_UPLOAD_LIMIT_INFO"), MessageType.Info);
            
            EditorGUILayout.Space();
            if (GUILayout.Button(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_LOGOUT_BUTTON")))
            { 
                bool logout = EditorUtility.DisplayDialog(
                    CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_LOGOUT_DIALOG_TITLE"),
                    CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_LOGOUT_DIALOG_BODY"),
                    CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_LOGOUT_DIALOG_ACCEPT"), 
                    CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_LOGOUT_DIALOG_DECLINE"));
                if (logout) Logout();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Community Resources", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_INFOTEXT_DOCUMENTATION"), MessageType.Info);
            if (GUILayout.Button(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_HEADING_DOCUMENTATION"))) 
                Application.OpenURL(DocumentationUrl);
            
            EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_WARNING_FEEDBACK"), MessageType.Info);
            if (GUILayout.Button(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_HEADING_FEEDBACK"))) 
                Application.OpenURL(FeedbackUrl);
            
            EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_WARNING_FOLDERPATH"), MessageType.Warning);
        }
    }
}*/