#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using CVR.CCKEditor.API;
using CVR.CCKEditor.ContentBuilder;
using CVR.CCKEditor.Tools;

public class BuilderState
{
    private const string ThumbnailFileName = "active-thumbnail.png";
    private const string PortalPanoramicFileName = "active-portal-panoramic.png";
    
    public static string ThumbnailCachePath 
        => Path.Combine(CCKCommonTools.SessionTempBuildOutputPath, ThumbnailFileName);
    
    public static string PortalPanoramicCachePath 
        => Path.Combine(CCKCommonTools.SessionTempBuildOutputPath, PortalPanoramicFileName);
    
    // SessionState keys
    private const string KeyPrefix = "CCK.Builder.";
    private const string KeySelectedLocalIdentifier = KeyPrefix + "SelectedLocalIdentifier";
    private const string KeyName = KeyPrefix + "Name";
    private const string KeyDescription = KeyPrefix + "Description";
    private const string KeyScreenEffects = KeyPrefix + "ScreenEffects";
    private const string KeyFlashingEffects = KeyPrefix + "FlashingEffects";
    private const string KeyJumpscare = KeyPrefix + "Jumpscare";
    private const string KeySuggestive = KeyPrefix + "Suggestive";
    private const string KeyViolence = KeyPrefix + "Violence";
    private const string KeyHorror = KeyPrefix + "Horror";
    private const string KeyGore = KeyPrefix + "Gore";
    private const string KeyExplicit = KeyPrefix + "Explicit";
    private const string KeyIsUploadPage = KeyPrefix + "IsUploadPage";
    private const string KeyChangelog = KeyPrefix + "Changelog";
    private const string KeyLegalCopyright = KeyPrefix + "LegalCopyright";
    private const string KeyLegalTags = KeyPrefix + "LegalTags";
    private const string KeyBuildPurpose = KeyPrefix + "BuildPurpose";
    
    #region Content Selection Properties
    
    public string SelectedLocalIdentifier
    {
        get => SessionState.GetString(KeySelectedLocalIdentifier, null);
        set => SessionState.SetString(KeySelectedLocalIdentifier, value);
    }

    #endregion Content Selection Properties

    #region Content Details Properties
    
    public string Name
    {
        get => SessionState.GetString(KeyName, string.Empty);
        set => SessionState.SetString(KeyName, value);
    }

    public string Description
    {
        get => SessionState.GetString(KeyDescription, string.Empty);
        set => SessionState.SetString(KeyDescription, value);
    }
    
    public bool SetNewThumbnail
    {
        get => SessionState.GetBool(KeyPrefix + "SetNewThumbnail", false);
        set => SessionState.SetBool(KeyPrefix + "SetNewThumbnail", value);
    }
    
    public bool SetNewPortalPanoramic
    {
        get => SessionState.GetBool(KeyPrefix + "SetNewPortalPanoramic", false);
        set => SessionState.SetBool(KeyPrefix + "SetNewPortalPanoramic", value);
    }
    
    private Texture2D _thumbnail;
    public Texture2D Thumbnail
    {
        get
        {
            if (!_thumbnail) LoadCachedImage(ThumbnailCachePath, ref _thumbnail);
            return _thumbnail;
        }
        set
        {
            if (_thumbnail && _thumbnail != value)
                UnityEngine.Object.DestroyImmediate(_thumbnail);
            
            _thumbnail = value;
            
            if (value)
                SaveImageToCache(_thumbnail, ThumbnailCachePath);
            else
                DeleteCachedImage(ThumbnailCachePath);
        }
    }

    private Texture2D _portalPanoramic;
    public Texture2D PortalPanoramic
    {
        get
        {
            if (!_portalPanoramic) LoadCachedImage(PortalPanoramicCachePath, ref _portalPanoramic);
            return _portalPanoramic;
        }
        set
        {
            if (_portalPanoramic && _portalPanoramic != value)
                UnityEngine.Object.DestroyImmediate(_portalPanoramic);
            
            _portalPanoramic = value;
            
            if (value)
                SaveImageToCache(_portalPanoramic, PortalPanoramicCachePath);
            else
                DeleteCachedImage(PortalPanoramicCachePath);
        }
    }

    #endregion

    #region Content Tags Properties
    
    public bool ScreenEffects
    {
        get => SessionState.GetBool(KeyScreenEffects, false);
        set => SessionState.SetBool(KeyScreenEffects, value);
    }

    public bool FlashingEffects
    {
        get => SessionState.GetBool(KeyFlashingEffects, false);
        set => SessionState.SetBool(KeyFlashingEffects, value);
    }

    public bool Jumpscare
    {
        get => SessionState.GetBool(KeyJumpscare, false);
        set => SessionState.SetBool(KeyJumpscare, value);
    }

    public bool Suggestive
    {
        get => SessionState.GetBool(KeySuggestive, false);
        set => SessionState.SetBool(KeySuggestive, value);
    }

    public bool Violence
    {
        get => SessionState.GetBool(KeyViolence, false);
        set => SessionState.SetBool(KeyViolence, value);
    }

    public bool Horror
    {
        get => SessionState.GetBool(KeyHorror, false);
        set => SessionState.SetBool(KeyHorror, value);
    }

    public bool Gore
    {
        get => SessionState.GetBool(KeyGore, false);
        set => SessionState.SetBool(KeyGore, value);
    }

    public bool Explicit
    {
        get => SessionState.GetBool(KeyExplicit, false);
        set => SessionState.SetBool(KeyExplicit, value);
    }

    #endregion

    #region Build/Upload Properties

    public BuildPurpose BuildPurpose
    {
        get => (BuildPurpose)SessionState.GetInt(KeyBuildPurpose, (int)BuildPurpose.OnlinePublish);
        set => SessionState.SetInt(KeyBuildPurpose, (int)value);
    }
    
    public bool IsUploadPage
    {
        get => SessionState.GetBool(KeyIsUploadPage, false);
        set => SessionState.SetBool(KeyIsUploadPage, value);
    }

    public string Changelog
    {
        get => SessionState.GetString(KeyChangelog, string.Empty);
        set => SessionState.SetString(KeyChangelog, value);
    }

    public bool LegalCopyrightAccepted
    {
        get => SessionState.GetBool(KeyLegalCopyright, false);
        set => SessionState.SetBool(KeyLegalCopyright, value);
    }

    public bool LegalTagsAccepted
    {
        get => SessionState.GetBool(KeyLegalTags, false);
        set => SessionState.SetBool(KeyLegalTags, value);
    }

    #endregion

    #region Thumbnail Handling

    private void LoadCachedImage(string filePath, ref Texture2D texture)
    {
        if (!File.Exists(filePath)) return;

        try
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (texture.LoadImage(fileData))
                return;
            
            Debug.LogError("[CCK] Failed to load cached thumbnail texture");
            UnityEngine.Object.DestroyImmediate(texture);
            texture = null;
        }
        catch (Exception e)
        {
            Debug.LogError($"[CCK] Error loading cached thumbnail: {e.Message}");
            texture = null;
        }
    }

    private static void SaveImageToCache(Texture2D texture, string filePath)
    {
        try
        {
            // Ensure directory exists
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            
            // Debug.Log($"[CCK] Saving thumbnail to cache: {ThumbnailCachePath}");

            // Save texture to PNG
            byte[] png = texture.EncodeToPNG();
            if (png != null) File.WriteAllBytes(filePath, png);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CCK] Error saving thumbnail to cache: {e.Message}");
        }
    }

    private static void DeleteCachedImage(string filePath)
    {
        try
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CCK] Error deleting cached thumbnail: {e.Message}");
        }
    }

    #endregion

    #region Helper Methods

    public void UpdateTags(UgcTagsData tags)
    {
        ScreenEffects = tags.ScreenEffects;
        FlashingEffects = tags.FlashingEffects;
        Jumpscare = tags.Jumpscare;
        Suggestive = tags.Suggestive;
        Violence = tags.Violence;
        Horror = tags.Horror;
        Gore = tags.Gore;
        Explicit = tags.Explicit;
    }

    public void ClearAll()
    {
        SelectedLocalIdentifier = null;
        Name = null;
        Description = null;
        Thumbnail = null;
        PortalPanoramic = null;
        SetNewThumbnail = false;
        SetNewPortalPanoramic = false;
        
        ScreenEffects = false;
        FlashingEffects = false;
        Jumpscare = false;
        Suggestive = false;
        Violence = false;
        Horror = false;
        Gore = false;
        Explicit = false;
        
        IsUploadPage = false;
        Changelog = null;
        LegalCopyrightAccepted = false;
        LegalTagsAccepted = false;
        
        BuildPurpose = BuildPurpose.OnlinePublish;
    }

    #endregion
}
#endif