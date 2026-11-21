using UnityEngine;

namespace CVR.CCKEditor.API
{
    public static class ApiCredentialsHandler
    {
        private const string KApiCredentialsUsernameKeyFormat = "m_ABI_Username_{0}";
        private const string KApiCredentialsAccessKeyKeyFormat = "m_ABI_Key_{0}";
        private const string KLastSelectedProfileKey = "m_ABI_LastProfile";
        private const int KMaxProfiles = 8;

        // Store credentials for all profiles in memory
        private static readonly string[] Usernames = new string[KMaxProfiles];
        private static readonly string[] AccessKeys = new string[KMaxProfiles];
        
        public static int CurrentProfile { get; private set; }

        // These now reference the current profile's credentials
        public static string Username => Usernames[CurrentProfile];
        public static string AccessKey => AccessKeys[CurrentProfile];

        public static bool IsCredentialsLoaded(int profile) 
            => !string.IsNullOrEmpty(Usernames[profile]) && !string.IsNullOrEmpty(AccessKeys[profile]);

        public static bool IsCurrentCredentialsLoaded() 
            => IsCredentialsLoaded(CurrentProfile);

        private static string GetUsernameKey(int profile) => string.Format(KApiCredentialsUsernameKeyFormat, profile);
        private static string GetAccessKeyKey(int profile) => string.Format(KApiCredentialsAccessKeyKeyFormat, profile);

        public static void LoadAllProfiles()
        {
            #if UNITY_EDITOR
            // Load last selected profile
            CurrentProfile = UnityEditor.EditorPrefs.GetInt(KLastSelectedProfileKey, 0);
            
            // Load all profiles' credentials
            for (int i = 0; i < KMaxProfiles; i++)
            {
                Usernames[i] = UnityEditor.EditorPrefs.GetString(GetUsernameKey(i), null);
                AccessKeys[i] = UnityEditor.EditorPrefs.GetString(GetAccessKeyKey(i), null);
            }
            #endif
        }

        public static void SwitchProfile(int profile)
        {
            if (profile is < 0 or >= KMaxProfiles)
                throw new System.ArgumentOutOfRangeException(nameof(profile));

            #if UNITY_EDITOR
            CurrentProfile = profile;
            UnityEditor.EditorPrefs.SetInt(KLastSelectedProfileKey, profile);
            #endif
        }

        public static void SaveCredentials(string username, string accessKey, int profile = -1)
        {
            if (profile == -1) profile = CurrentProfile;
            
            #if UNITY_EDITOR
            // Save to EditorPrefs
            UnityEditor.EditorPrefs.SetString(GetUsernameKey(profile), username);
            UnityEditor.EditorPrefs.SetString(GetAccessKeyKey(profile), accessKey);
            #endif

            // Update in-memory credentials
            Usernames[profile] = username;
            AccessKeys[profile] = accessKey;
        }

        public static void ClearCredentials(int profile = -1)
        {
            if (profile == -1) profile = CurrentProfile;
            
            #if UNITY_EDITOR
            UnityEditor.EditorPrefs.DeleteKey(GetUsernameKey(profile));
            UnityEditor.EditorPrefs.DeleteKey(GetAccessKeyKey(profile));
            #endif

            Usernames[profile] = null;
            AccessKeys[profile] = null;
        }

        public static void ClearAllProfiles()
        {
            #if UNITY_EDITOR
            for (int i = 0; i < KMaxProfiles; i++)
            {
                ClearCredentials(i);
            }
            UnityEditor.EditorPrefs.DeleteKey(KLastSelectedProfileKey);
            CurrentProfile = 0;
            #endif
        }

        public static (string username, string accessKey) GetCredentials(int profile)
        {
            return (Usernames[profile], AccessKeys[profile]);
        }
    }
}