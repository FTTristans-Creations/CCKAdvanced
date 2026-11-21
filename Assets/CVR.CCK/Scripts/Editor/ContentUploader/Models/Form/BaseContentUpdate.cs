namespace CVR.CCKEditor.ContentUploader.ContentUploaderModels.Form
{
    /// <summary>
    /// Details information for the content
    /// </summary>
    public sealed class ContentMetadata
    {
        /// <summary>
        /// Content Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Content Description
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Details information for the content file
    /// </summary>
    public class ContentFilesMetadata
    {
        /// <summary>
        /// Platform
        /// </summary>
        public UploadPlatforms Platform { get; set; }

        /// <summary>
        /// Content file changelog
        /// </summary>
        public string Changelog { get; set; }

        /// <summary>
        /// Content tags
        /// </summary>
        public ContentTags Tags { get; set; }

        public static UploadPlatforms GetCurrentPlatform()
        {
#if PLATFORM_ANDROID
            return UploadPlatforms.Android_Standalone;
#else
            return UploadPlatforms.Pc_Standalone;
#endif
        }
    }
}
