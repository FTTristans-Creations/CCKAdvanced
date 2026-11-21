#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using ABI.CCK.Components;
using UnityEditor;

/// <summary>
/// Contains all enabled asset info scripts categorized into the 3 content types.
/// This way we don't need to do resource/scene lookups to find a piece of content.
/// </summary>
public static class CCKAssetInfoManager
{
    public static Action onAssetInfoListChanged;
    
    public static readonly List<CVRAssetInfo> AllAssetInfos = new();
    private static readonly HashSet<string> LocalEditorIdentifiers = new();

    private static bool _isTracking = true;
    
    #region Public Methods
    
    public static void StartAssetInfoTracking() => _isTracking = true;
    public static void StopAssetInfoTracking() => _isTracking = false;
    
    public static void RegisterAssetInfo(CVRAssetInfo assetInfo)
    {
        if (!_isTracking) return;
        AssignLocalEditorIdentifierIfNeeded(assetInfo);
        
        AllAssetInfos.Add(assetInfo);
        LocalEditorIdentifiers.Add(assetInfo.localEditorIdentifier);
        ScheduleEventInvokeNextTickIfNeeded();
    }

    public static void UnregisterAssetInfo(CVRAssetInfo assetInfo)
    {
        AllAssetInfos.Remove(assetInfo);
        LocalEditorIdentifiers.Remove(assetInfo.localEditorIdentifier);
        ScheduleEventInvokeNextTickIfNeeded();
    }
    
    // TODO: this is broken again and rerolls every time. this needs to be a stable id
    private static void AssignLocalEditorIdentifierIfNeeded(CVRAssetInfo assetInfo)
    {
        // Check if the local editor identifier is empty or a dupe
        if (!string.IsNullOrEmpty(assetInfo.localEditorIdentifier)
            && !LocalEditorIdentifiers.Contains(assetInfo.localEditorIdentifier)) 
            return;
        
        // Assign a new editor identifier. This is purely for the Control Panel to be able to
        // reliably identify a CVRAssetInfo during the session after domain reload. Using the objectId
        // is not viable as it is normal to either be duplicated in scene or not yet assigned by api.
        assetInfo.localEditorIdentifier = Guid.NewGuid().ToString();
    }
    
    public static bool IsAssetInfoValid(CVRAssetInfo assetInfo) 
    {
        // Check if the ting exists
        if (!assetInfo)
            return false;
        
        // Check if the object is EditorOnly
        if (assetInfo.gameObject.CompareTag("EditorOnly"))
            return false;
        
        // Verify the asset info is a proper asset type
        if (assetInfo.type 
            is not CVRAssetInfo.AssetType.Avatar 
            and not CVRAssetInfo.AssetType.World 
            and not CVRAssetInfo.AssetType.Spawnable)
            return false;

        return true;
    }
    
    #endregion Public Methods

    #region Buffered Event Invoke

    private static bool _fireEventNextTick;

    // Schedule invoke next editor update, allowing multiple CVRAssetInfo to register this frame
    private static void ScheduleEventInvokeNextTickIfNeeded()
    {
        if (_fireEventNextTick) return;
        _fireEventNextTick = true;
        EditorApplication.update += InvokeEventAndConsumeFlag;
    }

    // Consume the scheduled invoke
    private static void InvokeEventAndConsumeFlag()
    {
        _fireEventNextTick = false;
        EditorApplication.update -= InvokeEventAndConsumeFlag;
        onAssetInfoListChanged?.Invoke();
    }

    #endregion Buffered Event Invoke
}
#endif