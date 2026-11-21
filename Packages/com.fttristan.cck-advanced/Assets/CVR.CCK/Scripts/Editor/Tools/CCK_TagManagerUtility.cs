using System.Linq;
using UnityEngine;
using UnityEditor;

public static class CCK_TagManagerUtility
{
    /// <summary>
    /// ChilloutVR layer configurations. Layers 0-5 are reserved by Unity.
    /// </summary>
    private static readonly (int Index, string Name)[] LayerConfigurations = {
        // Game layers, can be force renamed
        (6, "PassPlayer"), (7, "BlockPlayer"),
        (8, "PlayerLocal"), (9, "PlayerClone"), (10, "PlayerNetwork"),
        (11, "MirrorReflection"), (12, "Camera Only"),
        (13, "CVRReserved1"), (14, "CVRReserved2"), (15, "CVRReserved3"),
        // User Content world layers, do not normally force rename
        (16, "CVRContent1 (can be renamed)"), (17, "CVRContent2 (can be renamed)"),
        (18, "CVRContent3 (can be renamed)"), (19, "CVRContent4 (can be renamed)"),
        (20, "CVRContent5 (can be renamed)"), (21, "CVRContent6 (can be renamed)"),
        (22, "CVRContent7 (can be renamed)"), (23, "CVRContent8 (can be renamed)"),
        (24, "CVRContent9 (can be renamed)"), (25, "CVRContent10 (can be renamed)"),
        (26, "CVRContent11 (can be renamed)"), (27, "CVRContent12 (can be renamed)"),
        (28, "CVRContent13 (can be renamed)"), (29, "CVRContent14 (can be renamed)"),
        (30, "CVRContent15 (can be renamed)"), (31, "CVRContent16 (can be renamed)")
    };

    /// <summary>
    /// Legacy layer names that should be updated when found during a reset.
    /// </summary>
    private static readonly string[] LegacyLayerNames = {
        "PostProcessing",
        "CVRPickup",
        "CVRInteractable"
    };

    /// <summary>
    /// Validates if the TagManager settings are correct.
    /// </summary>
    public static bool ValidateTagManager()
    {
        // Check if the game layers are correctly named
        for (int i = 6; i <= 15; i++)
            if (LayerMask.LayerToName(i) != LayerConfigurations[i - 6].Name)
                return false;
        
        // Check if the user content layers are legacy layers
        // for (int i = 16; i <= 31; i++)
        //     if (LegacyLayerNames.Contains(LayerMask.LayerToName(i)))
        //         return false;

        return true;
    }

    /// <summary>
    /// Resets the TagManager to the correct configuration
    /// </summary>
    /// <param name="forceReset">If true, forces reset of all configurable layers regardless of current state</param>
    /// <returns>True if reset was successful</returns>
    public static bool ConfigureTagManager(bool forceReset = false)
    {
        try
        {
            var tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAsset == null || tagManagerAsset.Length == 0)
            {
                Debug.LogError("[CCK:TagManager] Failed to load TagManager asset.");
                return false;
            }

            SerializedObject tagManager = new(tagManagerAsset[0]);
            SerializedProperty layerProperty = tagManager.FindProperty("layers");

            if (layerProperty == null)
            {
                Debug.LogError("[CCK:TagManager] Failed to find layers property in TagManager.");
                return false;
            }

            foreach (var (Index, Name) in LayerConfigurations)
            {
                SerializedProperty layerEntry = layerProperty.GetArrayElementAtIndex(Index);
                if (layerEntry == null) continue;

                bool shouldUpdate = Index is >= 6 and <= 15; // Always update game layers
                
                if (!shouldUpdate)
                {
                    // Check if layer should be updated based on conditions
                    shouldUpdate = forceReset ||
                                 string.IsNullOrEmpty(layerEntry.stringValue) ||
                                 LegacyLayerNames.Contains(layerEntry.stringValue) ||
                                 layerEntry.stringValue.StartsWith("RCC_"); // RCC tries to reserve layers
                }

                if (shouldUpdate) layerEntry.stringValue = Name;
            }

            tagManager.ApplyModifiedProperties();
            Debug.Log("[CCK:TagManager] TagManager has been successfully updated.");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CCK:TagManager] Error while resetting TagManager: {ex.Message}");
            return false;
        }
    }
}