using System.Collections.Generic;

namespace CVR.CCKEditor.ContentUploader.ContentUploaderModels
{
    public class UploadStatusUpdateResponse
    {
        public ResponseType ResponseType { get; set; }

        public string Message { get; set; }

        public StatusUpdateData Data { get; set; }

        /// <summary>
        /// Data of a Status update
        /// </summary>
        public sealed class StatusUpdateData
        {
            /// <summary>
            /// Current status of the uploading progress
            /// </summary>
            public Step CurrentStep { get; set; }

            /// <summary>
            /// Progress within the current status (only asset validator status has this)
            /// The value goes between 0.0 and 1.0
            /// </summary>
            public float? CurrentStepProgress { get; set; }

            /// <summary>
            /// Step Index of the current status (index in Steps)
            /// </summary>
            public int CurrentStepIndex { get; set; }

            /// <summary>
            /// All steps for this upload process
            /// </summary>
            public List<Step> Steps { get; set; }
        }

        /// <summary>
        /// Represents a status step during the upload
        /// Current step ids and their descriptions:
        /// - VALIDATING_PAYLOAD_MISSING_ENTRIES - Checking for missing entries in the payload
        /// - VALIDATING_PAYLOAD_FILE_SIZE_AND_TYPE - Checking the size and type of the files in the payload
        /// - PROCESSING_ASSET_BUNDLE - Processing the asset bundle
        /// - STORING_CONTENT_METADATA - Storing the content metadata
        /// - ENCRYPTING_ASSET_BUNDLE - Encrypting the asset bundle
        /// - STORING_ASSET_BUNDLE - Storing the asset bundle
        /// - STORING_CONTENT_FILE_METADATA - Storing the content file metadata
        /// - PROCESSING_CONTENT_IMAGE - Processing the content image
        /// - STORING_CONTENT_IMAGE - Storing the content image
        /// - PROCESSING_WORLD_PANO_IMAGE - Processing the world pano image
        /// - STORING_WORLD_PANO_IMAGE - Storing the world pano image
        /// - FINISHING_UP - Finishing up things
        /// - FINISHED - Finished
        /// </summary>
        public struct Step
        {
            /// <summary>
            /// The id of the status, so it can be localized in the CCK
            /// </summary>
            public string Id { get; set; }

            /// <summary>
            /// The description of the status, so we have a fallback for it's meaning
            /// </summary>
            public string Description { get; set; }
        }
    }

    /// <summary>
    /// Response types for the Upload Websocket
    /// </summary>
    public enum ResponseType
    {
        /// <summary>
        /// Send a status update about the upload
        /// </summary>
        UploadStatusUpdate = 0
    }
}