using UnityEngine;
using UnityEngine.Serialization;

namespace CVR.CCKEditor.Localization
{
    /// <summary>
    /// Asset definition for CCK localization files.
    /// Allows for easily finding the localization data and associated image in the project.
    /// (Should mean no path requirement, easier to share amongst community)
    /// </summary>
    [CreateAssetMenu(fileName = "NewLocale", menuName = "CCK/Locale Asset")]
    public class CCKLocaleAsset : ScriptableObject
    {
        [FormerlySerializedAs("IdentifierName")] [Header("Locale Settings")]
        // public Texture2D IdentifierImage;
        public string identifierName;
        [FormerlySerializedAs("LocalizationData")] public TextAsset localizationData;
    }
}