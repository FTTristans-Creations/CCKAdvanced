using System;
using ABI.CCK.Components;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CVR.CCKEditor.ContentBuilder
{
    public class PrefabTempBuildAsset : TempBuildAsset
    {
        #region Initialization
        
        protected override void CreateFromAssetInfo(CVRAssetInfo assetInfo, BuildPurpose buildPurpose)
        {
            // Instantiate the root object so we can save it as a prefab
            RootObject = Object.Instantiate(assetInfo.gameObject);
            
            // Unpack all prefab instances so we can save the prefab as a new asset
            if (PrefabUtility.IsPartOfNonAssetPrefabInstance(RootObject) && PrefabUtility.IsOutermostPrefabInstanceRoot(RootObject))
                PrefabUtility.UnpackPrefabInstance(RootObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            // Remove the EditorOnly tag from the object root... people do this for some reason
            if (RootObject.CompareTag("EditorOnly")) RootObject.tag = "Untagged";
            
            // TODO: Unity has a built-in neat method to handle this. Unsure though if there is any benefit as we already
            // have the prefab instantiated at this point.
            // https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PrefabUtility.LoadPrefabContents.html
            AssetInfo = RootObject.GetComponent<CVRAssetInfo>();
            TempAssetPath = "Assets/" + GetAssetNameFromInfo(assetInfo, buildPurpose);
        }
        
        #endregion Initialization

        #region TempBuildAsset Implementation
        
        public override void SaveChangesToAsset()
        {
            PrefabUtility.SaveAsPrefabAsset(RootObject, TempAssetPath);
        }

        public override void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            
            if (RootObject)
            {
                try { Object.DestroyImmediate(RootObject); }
                catch (Exception e) { Debug.LogException(e); }
                RootObject = null;
            }
            
            if (!string.IsNullOrEmpty(TempAssetPath))
            {
                try { AssetDatabase.DeleteAsset(TempAssetPath); }
                catch (Exception e) { Debug.LogException(e); }
                TempAssetPath = null;
            }
        }
        
        public override T[] GetAllComponents<T>()
        {
            return RootObject == null 
                ? Array.Empty<T>() 
                : RootObject.GetComponentsInChildren<T>(true);
        }
        
        #endregion TempBuildAsset Implementation
    }
}