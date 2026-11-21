using CVR.CCKEditor.API;
using CVR.CCKEditor.ContentUploader.ContentUploaderModels.Form;

namespace CVR.CCKEditor.ContentBuilder
{
    public class UploadInfo
    {
        #region Info
        
        public string Name { get; set; }
        public string Description { get; set; }
        public UgcTagsData ContentTags { get; set; }

        public ContentMetadata ContentMeta { get; set; }

        public ContentFilesMetadata ContentFilesMeta { get; set; }

        public string Changelog { get; set; }
        
        /// <summary>
        /// Whether the bundle should be set active after upload. If false, the bundle
        /// will still be uploaded, but loading the content in-game will use the prior active bundle.
        /// </summary>
        /// <remarks>
        /// If you upload a new asset with this false, it will not be searchable or viewable in-game.
        /// It will only be accessible via the Hub.
        /// </remarks>
        public bool SetAsActiveFile { get; set; } = true;
        
        /// <summary>
        /// The contents main image. Provide a path if you want to set/replace the current image.
        /// </summary>
        public string ThumbnailImagePath { get; set; }
        
        /// <summary>
        /// The panoramic image used for in-game portal previews, expected to be 4096x4096.
        /// If not provided, one will be generated automatically at world spawn during build.
        /// WARNING: This expects a filepath where there are two files postfixed "_1024.png" and "_4096.png"...
        /// </summary>
        public string PanoramicImagePath { get; set; }
        
        #endregion Info

        #region Properties
        
        /// <summary>
        /// Returns true if all required fields are filled in.
        /// </summary>
        public bool HasAllRequiredFields
            => !string.IsNullOrEmpty(Name);
        
        #endregion Properties
    }
}