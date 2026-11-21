using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ABI.CCK.Components;
using CVR.CCKEditor.Tools;
using UnityEditor;
using UnityEngine;

namespace CVR.CCKEditor.ContentBuilder
{
    internal class ContentTester : IContentBuildHandler
    {
        private static string LastBundlePath
        {
            get => SessionState.GetString("CCK.ContentTester.LastBundlePath", string.Empty);
            set => SessionState.SetString("CCK.ContentTester.LastBundlePath", value);
        }
        
        private CVRAssetInfo _assetInfo;
        private int _randomNum;
        
        public Task PrepareForBuild(CVRAssetInfo assetInfo, CancellationToken cancellationToken)
        {
            // We don't really have any preprocessing
            _assetInfo = assetInfo;
            
            // Set random number
            assetInfo.randomNum = new System.Random().Next(11111111, 99999999).ToString();
            EditorUtility.SetDirty(assetInfo);
            
            // Delete the last bundle path if it exists
            CCKCommonTools.DeleteBundleAndManifest(LastBundlePath);
            
            return Task.CompletedTask;
        }

        public Task RunAfterBuild(string bundlePath, Action<string, float> onProgressUpdated, CancellationToken cancellationToken)
        {
            // Get full OS path to bundle
            bundlePath = Path.GetFullPath(bundlePath);
            
            string contentType = _assetInfo.type.ToString().ToLower();
            string protocol = $"chilloutvr://test/{contentType}?objectId={_assetInfo.localEditorIdentifier}&filepath={Uri.EscapeDataString(bundlePath)}";
            
            // Hit the Local Test deeplink!
            Application.OpenURL(protocol);
         
            // Save the last bundle path so we can nuke it later
            LastBundlePath = bundlePath;
            
            return Task.CompletedTask;
        }
    }
}