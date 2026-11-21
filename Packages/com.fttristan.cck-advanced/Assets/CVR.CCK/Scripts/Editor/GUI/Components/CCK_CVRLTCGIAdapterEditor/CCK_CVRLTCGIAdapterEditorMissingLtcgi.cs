#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    public partial class CCK_CVRLTCGIAdapterEditor
    {
        private static AddRequest _addRequest;
        
        private void Draw_MissingLtcgi()
        {
            using (new LabelScope(L("MissingLtcgi").text))
                DrawMissingLtcgi();
        }

        #region Drawing Methods

        private void DrawMissingLtcgi()
        {
            EditorGUILayout.HelpBox(L("LtcgiNotFoundError").text, MessageType.Error);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(L("OpenLtcgiRepo").text)) Application.OpenURL(KAFE_LTCGI_REPO_URL);
                    
                bool isUpmImportInProgress = _addRequest is { IsCompleted: false };
                using (new EditorGUI.DisabledScope(isUpmImportInProgress))
                {
                    string buttonText = isUpmImportInProgress ? L("Importing").text : L("ImportLtcgiViaUpm").text;
                    if (GUILayout.Button(buttonText))
                    {
                        _addRequest = Client.Add(KAFE_LTCGI_GIT_URL);
                        EditorApplication.update += ProgressCheck;
                    }
                }

            }
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(L("UpmInstallationFailInfo").text, MessageType.Info);
            if (GUILayout.Button(L("DownloadLtcgiUnityPackage").text))
                Application.OpenURL(KAFE_LTCGI_GIT_RELEASES_URL);
        }

        #endregion Drawing Methods
        
        #region Progress Check
        
        private static void ProgressCheck()
        {
            if (!_addRequest.IsCompleted) 
                return;

            if (_addRequest.Status == StatusCode.Success) 
                Debug.Log("Installed: " + _addRequest.Result.packageId);
            else if (_addRequest.Status >= StatusCode.Failure) 
                Debug.LogError("Failed to install package: " + _addRequest.Error.message);

            _addRequest = null;
            EditorApplication.update -= ProgressCheck;
        }
        
        #endregion Progress Check
    }
}
#endif