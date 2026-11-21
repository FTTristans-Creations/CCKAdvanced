/*using System.Collections.Generic;
using ABI.CCK.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ABI.CCK.Scripts.Editor
{
    public partial class CCK_BuildManagerWindow
    {
        private const string SessionKeyUploadMode = "CCK_UploadMode";
        private static int UploadMode
        {
            get => SessionState.GetInt(SessionKeyUploadMode, 0);
            set => SessionState.SetInt(SessionKeyUploadMode, Mathf.Max(0, value));
        }

        private static readonly string[] UploadModeOptions = { "Online Upload", "Local Test" };

        #region Avatar Validation
        
        private void OnGUIAvatar(CVRAvatar avatar)
        {
            GameObject avatarObject = avatar.gameObject;
            
            int errors = 0;
            int overallPolygonsCount = 0;
            int overallSkinnedMeshRenderer = 0;
            int overallUniqueMaterials = 0;

            foreach (MeshFilter filter in avatar.gameObject.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh != null) overallPolygonsCount += filter.sharedMesh.triangles.Length / 3;
            }
            
            foreach (SkinnedMeshRenderer renderer in avatar.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                overallSkinnedMeshRenderer++;
                if (renderer.sharedMaterials != null) overallUniqueMaterials += renderer.sharedMaterials.Length;
            }
            
            var overallMissingScripts = CCK_Tools.CleanMissingScripts(CCK_Tools.SearchType.Selection, false, avatarObject);
            if (overallMissingScripts > 0) errors++;

            if (overallMissingScripts > 0) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_ERROR_AVATAR_MISSING_SCRIPTS"), MessageType.Error);
            
            Animator animator = avatar.GetComponent<Animator>();
            if (animator == null)
            {
                errors++;
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_AVATAR_ERROR_ANIMATOR"), MessageType.Error);
            }
            
            if (animator != null && animator.avatar == null)
            {
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_AVATAR_WARNING_GENERIC"), MessageType.Warning);
            }
            
            List<Texture> referencedTextures = GetAllReferencedTextures(avatarObject);
            if (CheckForInvalidTextureSize(referencedTextures))
            {
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_AVATAR_ERROR_TOO_LARGE_TEXTURE"), MessageType.Error);
                if(GUILayout.Button(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_FIX_IMPORT_SETTINGS")))
                {
                    AdjustMaxTextureSizes(referencedTextures);
                }
                errors++;
            }
            
            if (CheckForInvalidStreamingMipMappingSettings(referencedTextures))
            {
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_AVATAR_ERROR_INVALID_STREAMING_MIPMAPS"), MessageType.Error);
                if(GUILayout.Button(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_FIX_IMPORT_SETTINGS")))
                {
                    AdjustStreamingMipMappingSettings(referencedTextures);
                }
                errors++;
            }

            List<AudioClip> referencedAudioClips = GetAllReferencedAudioClips(avatarObject);
            if (CheckForInvalidLoadInBackground(referencedAudioClips))
            {
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_AVATAR_ERROR_AUDIO_LOAD_IN_BACKGROUND"), MessageType.Error);
                if(GUILayout.Button(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_FIX_IMPORT_SETTINGS")))
                {
                    AdjustLoadInBackground(referencedAudioClips);
                }
                errors++;
            }
            
            if (overallPolygonsCount > 100000) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_AVATAR_WARNING_POLYGONS").Replace("{X}", overallPolygonsCount.ToString()), MessageType.Warning);
            
            if (overallSkinnedMeshRenderer > 10) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_AVATAR_WARNING_SKINNED_MESH_RENDERERS").Replace("{X}", overallSkinnedMeshRenderer.ToString()), MessageType.Warning);
            
            if (overallUniqueMaterials > 20) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_AVATAR_WARNING_MATERIALS").Replace("{X}", overallUniqueMaterials.ToString()), MessageType.Warning);
            
            if (avatar.viewPosition == Vector3.zero) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_AVATAR_WARNING_VIEWPOINT"), MessageType.Warning);
            
            if (animator != null && animator.avatar != null && !animator.avatar.isHuman) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_AVATAR_WARNING_NON_HUMANOID"), MessageType.Warning);
            
            List<string> avatarMeshes = getAllAssetMeshesInAvatar(avatarObject);
            if (CheckForLegacyBlendShapeNormals(avatarMeshes))
            {
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_AVATAR_WARNING_LEGACY_BLENDSHAPES"), MessageType.Warning);
                if(GUILayout.Button(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_FIX_IMPORT_SETTINGS")))
                {
                    FixLegacyBlendShapeNormals(avatarMeshes);
                }
            }

            if (overallPolygonsCount is >= 50000 and <= 100000) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_AVATAR_INFO_POLYGONS").Replace("{X}", overallPolygonsCount.ToString()), MessageType.Info);
            
            if (overallSkinnedMeshRenderer is >= 5 and <= 10) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_AVATAR_INFO_SKINNED_MESH_RENDERERS").Replace("{X}", overallSkinnedMeshRenderer.ToString()), MessageType.Info);
            
            if (overallUniqueMaterials is >= 10 and <= 20) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_AVATAR_INFO_MATERIALS").Replace("{X}", overallUniqueMaterials.ToString()), MessageType.Info);
            
            if (avatar.viewPosition.y <= 0.5f) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_AVATAR_INFO_SMALL"), MessageType.Info);
            
            if (avatar.viewPosition.y > 3f) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_AVATAR_INFO_HUGE"), MessageType.Info);
            
            if (overallMissingScripts > 0)
            {
                if (GUILayout.Button(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_UTIL_REMOVE_MISSING_SCRIPTS_BUTTON"))) 
                    CCK_Tools.CleanMissingScripts(CCK_Tools.SearchType.Selection, true, avatarObject);
            }

            if (errors > 0)
                return;
            
            GUILayout.FlexibleSpace();
                
            DisplayUploadModeHelp();
                
            string buttonText = UploadMode == 0 
                ? CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_AVATAR_UPLOAD_BUTTON") 
                : "Build Local Test Avatar";
                
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(buttonText)) 
                _ = CCK_BuildUtility.BuildAndUploadAvatar(avatarObject, (CCK_BuildUtility.BuildReason)UploadMode);
                
            UploadMode = EditorGUILayout.Popup(UploadMode, UploadModeOptions, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
        }
        
        #endregion Avatar Validation

        #region Spawnable Validation

        private void OnGUISpawnable(CVRSpawnable s)
        {
            GameObject spawnableObject = s.gameObject;
            
            int errors = 0;
            int overallPolygonsCount = 0;
            int overallSkinnedMeshRenderer = 0;
            int overallUniqueMaterials = 0;

            foreach (MeshFilter filter in s.gameObject.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh != null) overallPolygonsCount += filter.sharedMesh.triangles.Length / 3;
            }
            
            foreach (SkinnedMeshRenderer renderer in s.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                overallSkinnedMeshRenderer++;
                if (renderer.sharedMaterials != null) overallUniqueMaterials += renderer.sharedMaterials.Length;
            }
            
            var overallMissingScripts = CCK_Tools.CleanMissingScripts(CCK_Tools.SearchType.Selection, false, spawnableObject);
            if (overallMissingScripts > 0) errors++;

            if (overallMissingScripts > 0) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_PROPS_ERROR_MISSING_SCRIPT"), MessageType.Error);
            
            if (overallPolygonsCount > 100000) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_PROPS_WARNING_POLYGONS").Replace("{X}", overallPolygonsCount.ToString()), MessageType.Warning);
            
            if (overallSkinnedMeshRenderer > 10) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_PROPS_WARNING_SKINNED_MESH_RENDERERS").Replace("{X}", overallSkinnedMeshRenderer.ToString()), MessageType.Warning);
            
            if (overallUniqueMaterials > 20) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_PROPS_WARNING_MATERIALS").Replace("{X}", overallUniqueMaterials.ToString()), MessageType.Warning);

            List<string> spawnableMeshes = getAllAssetMeshesInAvatar(spawnableObject);
            if (CheckForLegacyBlendShapeNormals(spawnableMeshes))
            {
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_PROPS_WARNING_LEGACY_BLENDSHAPES"), MessageType.Warning);
                if(GUILayout.Button(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_FIX_IMPORT_SETTINGS")))
                {
                    FixLegacyBlendShapeNormals(spawnableMeshes);
                }
            }

            List<Texture> referencedTextures = GetAllReferencedTextures(spawnableObject);
            if (CheckForInvalidTextureSize(referencedTextures))
            {
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_PROPS_ERROR_TOO_LARGE_TEXTURE"), MessageType.Error);
                if(GUILayout.Button(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_FIX_IMPORT_SETTINGS")))
                {
                    AdjustMaxTextureSizes(referencedTextures);
                }
                errors++;
            }
            
            if (CheckForInvalidStreamingMipMappingSettings(referencedTextures))
            {
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_PROPS_ERROR_INVALID_STREAMING_MIPMAPS"), MessageType.Error);
                if(GUILayout.Button(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_FIX_IMPORT_SETTINGS")))
                {
                    AdjustStreamingMipMappingSettings(referencedTextures);
                }
                errors++;
            }

            List<AudioClip> referencedAudioClips = GetAllReferencedAudioClips(spawnableObject);
            if (CheckForInvalidLoadInBackground(referencedAudioClips))
            {
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_PROPS_ERROR_AUDIO_LOAD_IN_BACKGROUND"), MessageType.Error);
                if(GUILayout.Button(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_FIX_IMPORT_SETTINGS")))
                {
                    AdjustLoadInBackground(referencedAudioClips);
                }
                errors++;
            }

            if (overallPolygonsCount is >= 50000 and <= 100000) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_PROPS_INFO_POLYGONS").Replace("{X}", overallPolygonsCount.ToString()), MessageType.Info);
            
            if (overallSkinnedMeshRenderer is >= 5 and <= 10) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_PROPS_INFO_SKINNED_MESH_RENDERERS").Replace("{X}", overallSkinnedMeshRenderer.ToString()), MessageType.Info);
            
            if (overallUniqueMaterials is >= 10 and <= 20) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_PROPS_INFO_MATERIALS").Replace("{X}", overallUniqueMaterials.ToString()), MessageType.Info);

            if (overallMissingScripts > 0)
            {
                if (GUILayout.Button(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_UTIL_REMOVE_MISSING_SCRIPTS_BUTTON"))) 
                    CCK_Tools.CleanMissingScripts(CCK_Tools.SearchType.Selection, true, spawnableObject);
            }

            if (errors > 0)
                return;
            
            GUILayout.FlexibleSpace();
            
            DisplayUploadModeHelp();
                
            string buttonText = UploadMode == 0 
                ? CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_PROPS_UPLOAD_BUTTON") 
                : "Build Local Test Spawnable";
                
            EditorGUILayout.BeginHorizontal();
            
            // TODO: Spawnables need handling on the client to handle local testing, awaiting GS2

            if (UploadMode != 1)
            {
                if (GUILayout.Button(buttonText))
                    _ = CCK_BuildUtility.BuildAndUploadSpawnable(spawnableObject,
                        (CCK_BuildUtility.BuildReason)UploadMode);
            }
            else
            {
                Rect controlRect = EditorGUILayout.GetControlRect();
                EditorGUI.HelpBox(controlRect, "Local Testing is not yet supported for Props... Sorry :(",
                    MessageType.Error);
            }

            UploadMode = EditorGUILayout.Popup(UploadMode, UploadModeOptions, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
        }
        
        #endregion Spawnable Validation

        #region World Validation
        
        private void DisplayWorldValidation(ContentItem worldContent)
        {
            CVRWorld world = worldContent.GameObject.GetComponent<CVRWorld>();
            EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_INFOTEXT_WORLDS_NO_AVATARS"), MessageType.Info);

            int errors = 0;
            int overallMissingScripts = CCK_Tools.CleanMissingScripts(CCK_Tools.SearchType.Scene, false, null);
            if (overallMissingScripts > 0) errors++;
            
            if (overallMissingScripts > 0) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_ERROR_WORLD_MISSING_SCRIPTS"), MessageType.Error);
            
            if (world.spawns.Length == 0) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_WORLDS_WARNING_SPAWNPOINT"), MessageType.Warning);

            if (world.referenceCamera == null) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_WORLDS_INFO_REFERENCE_CAMERA"), MessageType.Info);
            
            if (world.respawnHeightY <= -500) 
                EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_WORLDS_INFO_RESPAWN_HEIGHT"), MessageType.Info);
            
            if (overallMissingScripts > 0)
            {
                if (GUILayout.Button(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_UTIL_REMOVE_MISSING_SCRIPTS_BUTTON"))) 
                    CCK_Tools.CleanMissingScripts(CCK_Tools.SearchType.Scene, true, null);
            }
            
            if (errors > 0) 
                return;
            
            GUILayout.FlexibleSpace();
            
            DisplayUploadModeHelp();
                
            string buttonText = UploadMode == 0 
                ? CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_UPLOAD_WORLD_BUTTON")
                : "Build Local Test World";
                
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(buttonText))
                _ = CCK_BuildUtility.BuildAndUploadMapAsset(SceneManager.GetActiveScene(), world.gameObject, (CCK_BuildUtility.BuildReason)UploadMode);
                
            UploadMode = EditorGUILayout.Popup(UploadMode, UploadModeOptions, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
        }
        
        #endregion World Validation

        #region Upload Mode UI

        private static void DisplayUploadModeHelp()
        {
            string labelText;
            string helpText;
            
            switch (UploadMode)
            {
                default:
                    labelText = "Upload Your Content";
                    helpText = "Builds and uploads your content to ChilloutVR.";
                    break;
                case 1:
                    labelText = "Local Test Build";
                    helpText = "Builds and immediately loads your content for quick testing.";
                    break;
            }
            
            EditorGUILayout.LabelField(labelText, EditorStyles.boldLabel);
            Rect controlRect = EditorGUILayout.GetControlRect();
            EditorGUI.HelpBox(controlRect, helpText, MessageType.None);
        }

        #endregion Upload Mode UI

        #region Shared Validation Helpers

        private static List<string> getAllAssetMeshesInAvatar(GameObject avatar)
        {
            List<string> assetPathList = new();

            foreach (SkinnedMeshRenderer sMeshRenderer in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if(sMeshRenderer == null) continue;
                
                Mesh currentMesh = sMeshRenderer.sharedMesh;
                
                if(currentMesh == null)
                {
                    Debug.LogWarning(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_WARNING_MESH_FILTER_MESH_EMPTY") + $": {sMeshRenderer.transform.name}");
                    continue;
                }
                
                if(!AssetDatabase.Contains(currentMesh)) continue;

                string meshAssetPath = AssetDatabase.GetAssetPath(currentMesh);
                if(string.IsNullOrEmpty(meshAssetPath)) continue;
                
                if (assetPathList.Contains(meshAssetPath)) continue;
                
                assetPathList.Add(meshAssetPath);
            }
            
            foreach (MeshFilter meshFilter in avatar.GetComponentsInChildren<MeshFilter>(true))
            {
                if(meshFilter == null) continue;
                
                Mesh currentMesh = meshFilter.sharedMesh;
                
                if(currentMesh == null)
                {
                    Debug.LogWarning(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_WARNING_MESH_FILTER_MESH_EMPTY") + $": {meshFilter.transform.name}");
                    continue;
                }
                
                if(!AssetDatabase.Contains(currentMesh)) continue;

                string meshAssetPath = AssetDatabase.GetAssetPath(currentMesh);
                if(string.IsNullOrEmpty(meshAssetPath)) continue;
                
                if (assetPathList.Contains(meshAssetPath)) continue;
                
                assetPathList.Add(meshAssetPath);
            }

            foreach (ParticleSystemRenderer pRenderer in avatar.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                if(pRenderer == null) continue;

                Mesh[] particleMeshes = new Mesh[pRenderer.meshCount];
                pRenderer.GetMeshes(particleMeshes);

                foreach (Mesh particleMesh in particleMeshes)
                {
                    if(particleMesh == null)
                    {
                        Debug.LogWarning(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_WARNING_MESH_FILTER_MESH_EMPTY") + $": {pRenderer.transform.name}");
                        continue;
                    }
                    
                    if(!AssetDatabase.Contains(particleMesh)) continue;

                    string meshAssetPath = AssetDatabase.GetAssetPath(particleMesh);
                    if(string.IsNullOrEmpty(meshAssetPath)) continue;
                
                    if (assetPathList.Contains(meshAssetPath)) continue;
                
                    assetPathList.Add(meshAssetPath);
                }
            }
            
            return assetPathList;
        }
        
        private static List<Texture> GetAllReferencedTextures(GameObject obj)
        {
            IEnumerable<Component> allComponents = obj.GetComponentsInChildren<Component>(true);
            
            List<Material> materials = new();

            foreach (Component component in allComponents)
            {
                switch (component)
                {
                    case Renderer renderer:
                        materials.AddRange(renderer.sharedMaterials);
                        break;
                    case Animator animator:
                        ParseAnimator(animator.runtimeAnimatorController);
                        break;
                    case CVRAvatar avatar:
                    {
                        if (avatar.overrides != null)
                            ParseAnimator(avatar.overrides.runtimeAnimatorController);
                        break;
                    }
                }
            }

            List<Texture> textures = new();

            foreach (Material material in materials)
            {
                if (material == null) 
                    continue;
                
                foreach (string textureProperty in material.GetTexturePropertyNames())
                {
                    Texture texture = material.GetTexture(textureProperty);
                    if (texture == null) 
                        continue;
                    
                    textures.Add(texture);
                }
            }
            
            return textures;
            void ParseAnimator(RuntimeAnimatorController animator)
            {
                if (animator == null) return;
                
                foreach (AnimationClip animationClip in animator.animationClips)
                {
                    EditorCurveBinding[] curveBindings = AnimationUtility.GetObjectReferenceCurveBindings(animationClip);

                    foreach (EditorCurveBinding curveBinding in curveBindings)
                    {
                        if (!curveBinding.isPPtrCurve) 
                            continue;

                        if (!curveBinding.type.IsSubclassOf(typeof(Renderer))) 
                            continue;

                        if (!curveBinding.propertyName.StartsWith("m_Material"))
                            continue;
                    
                        ObjectReferenceKeyframe[] referenceKeyframes = AnimationUtility.GetObjectReferenceCurve(animationClip, curveBinding);

                        foreach (ObjectReferenceKeyframe keyframe in referenceKeyframes) 
                            materials.Add(keyframe.value as Material);
                    }
                }
            }
        }
        
        private static bool CheckForInvalidTextureSize(List<Texture> textures)
        {
            foreach (Texture texture in textures)
            {
                if (texture == null) 
                    continue;
                
                string texturePath = AssetDatabase.GetAssetPath(texture);
                if (string.IsNullOrWhiteSpace(texturePath))
                    continue;

                AssetImporter importer = AssetImporter.GetAtPath(texturePath);
                if (importer is not TextureImporter textureImporter) 
                    continue;

                if (textureImporter.maxTextureSize > 8192)
                    return true;
            }
            
            return false;
        }

        private void AdjustMaxTextureSizes(List<Texture> textures)
        {
            foreach (Texture texture in textures)
            {
                if (texture == null) 
                    continue;
                
                string texturePath = AssetDatabase.GetAssetPath(texture);
                if (string.IsNullOrWhiteSpace(texturePath))
                    continue;

                AssetImporter importer = AssetImporter.GetAtPath(texturePath);
                if (importer is not TextureImporter textureImporter)
                    continue;

                if (textureImporter.maxTextureSize <= 8192)
                    continue;
                    
                textureImporter.maxTextureSize = 8192;
                importer.SaveAndReimport();
            }
        }
        
        private static bool CheckForInvalidStreamingMipMappingSettings(List<Texture> textures)
        {
            foreach (Texture texture in textures)
            {
                if (texture == null)
                    continue;
                
                string texturePath = AssetDatabase.GetAssetPath(texture);
                if (string.IsNullOrWhiteSpace(texturePath)) 
                    continue;

                AssetImporter importer = AssetImporter.GetAtPath(texturePath);
                if (importer is not TextureImporter textureImporter) 
                    continue;

                if (!textureImporter.mipmapEnabled) 
                    continue;

                if (!textureImporter.streamingMipmaps)
                    return true;
            }
            
            return false;
        }
        
        private static void AdjustStreamingMipMappingSettings(List<Texture> textures)
        {
            foreach (Texture texture in textures)
            {
                if (texture == null)
                    continue;
                
                string texturePath = AssetDatabase.GetAssetPath(texture);
                if (string.IsNullOrWhiteSpace(texturePath)) 
                    continue;

                AssetImporter importer = AssetImporter.GetAtPath(texturePath);
                if (importer is not TextureImporter textureImporter) 
                    continue;

                if (!textureImporter.mipmapEnabled) 
                    continue;

                if (textureImporter.streamingMipmaps)
                    continue;
                
                textureImporter.streamingMipmaps = true;
                importer.SaveAndReimport();
            }
        }

        private static List<AudioClip> GetAllReferencedAudioClips(GameObject obj)
        {
            IEnumerable<Component> allComponents = obj.GetComponentsInChildren<Component>(true);

            List<AudioClip> audioClips = new();

            foreach (Component component in allComponents)
            {
                switch (component)
                {
                    case AudioSource audioSource:
                        audioClips.Add(audioSource.clip);
                        break;
                    case CVRAudioDriver audioDriver:
                    {
                        AudioSource driverAudioSource = audioDriver.audioSource;
                        if (driverAudioSource != null) 
                            audioClips.Add(driverAudioSource.clip);
                    
                        audioClips.AddRange(audioDriver.audioClips);
                        break;
                    }
                    case CVRParticleSound particleSound:
                    {
                        AudioSource dieReference = particleSound.dieAudioSourceReference;
                        if (dieReference != null)
                            audioClips.Add(dieReference.clip);
                    
                        AudioSource spawnReference = particleSound.dieAudioSourceReference;
                        if (spawnReference != null)
                            audioClips.Add(spawnReference.clip);
                    
                        audioClips.AddRange(particleSound.spawnSound);
                        audioClips.AddRange(particleSound.dieSound);
                        break;
                    }
                    case GunController gunController:
                        audioClips.AddRange(gunController.reloadSounds);
                        audioClips.AddRange(gunController.shootSounds);
                        audioClips.AddRange(gunController.emptyShootSounds);
                        break;
                }
            }

            return audioClips;
        }

        private static bool CheckForInvalidLoadInBackground(List<AudioClip> audioClips)
        {
            foreach (AudioClip audioClip in audioClips)
            {
                if (audioClip == null)
                    continue;
                
                string audioClipPath = AssetDatabase.GetAssetPath(audioClip);
                if (string.IsNullOrWhiteSpace(audioClipPath)) 
                    continue;

                AssetImporter importer = AssetImporter.GetAtPath(audioClipPath);
                if (importer is not AudioImporter audioImporter)
                    continue;

                if (!audioImporter.loadInBackground)
                    return true;
            }

            return false;
        }
        
        private static void AdjustLoadInBackground(List<AudioClip> audioClips)
        {
            foreach (AudioClip audioClip in audioClips)
            {
                if (audioClip == null) 
                    continue;
                
                string audioClipPath = AssetDatabase.GetAssetPath(audioClip);
                if (string.IsNullOrWhiteSpace(audioClipPath))
                    continue;

                AssetImporter importer = AssetImporter.GetAtPath(audioClipPath);
                if (importer is not AudioImporter audioImporter)
                    continue;

                if (audioImporter.loadInBackground) 
                    continue;

                audioImporter.loadInBackground = true;
                audioImporter.SaveAndReimport();
            }
        }
        
        private static bool CheckForLegacyBlendShapeNormals(List<string> assetPaths)
        {
            foreach (string assetPath in assetPaths)
            {
                ModelImporter modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if(modelImporter == null)
                    continue;

                if(modelImporter.importBlendShapeNormals != ModelImporterNormals.Calculate)
                    continue;
                
                if((bool)legacyBlendShapeImporter.GetValue(modelImporter)) 
                    continue;

                return true;
            }

            return false;
        }

        private static void FixLegacyBlendShapeNormals(List<string> assetPaths)
        {
            foreach (string assetPath in assetPaths)
            {
                ModelImporter modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if(modelImporter == null) 
                    continue;

                if(modelImporter.importBlendShapeNormals != ModelImporterNormals.Calculate) 
                    continue;

                legacyBlendShapeImporter.SetValue(modelImporter, true);
                modelImporter.SaveAndReimport();
            }
        }
        
        #endregion Shared Validation Helpers
    }
}*/