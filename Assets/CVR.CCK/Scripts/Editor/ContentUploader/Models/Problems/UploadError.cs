using System.Collections.Generic;
using System.Net;

namespace CVR.CCKEditor.ContentUploader.ContentUploaderModels.Problems
{
    /// <summary>
    /// Possible errors that occur during upload
    /// </summary>
    public static class UploadError
    {
        /// <summary>
        /// Category for the upload errors
        /// </summary>
        public const string Category = "Upload";

        /// <summary>
        /// Generic internal server error
        /// </summary>
        public static Problem UnknownError => new($"{Category}.UnknownError", "An unknown error occurred.", HttpStatusCode.InternalServerError);

        /// <summary>
        /// Asset Guardian internal server error during asset bundle validation
        /// </summary>
        public static Problem UnknownAssetBundleError => new($"{Category}.UnknownAssetBundleError", "An unknown error occurred during the asset bundle processing.", HttpStatusCode.InternalServerError);

        /// <summary>
        /// Represent a problem where the content id provided was not found in the database
        /// </summary>
        public static Problem ContentNotFound => new Problem($"{Category}.NotFound", "The provided content id was not found.", HttpStatusCode.NotFound);

        /// <summary>
        /// Represent a problem where the content id provided belongs to another user
        /// </summary>
        public static Problem NoPermissionsForContent => new Problem($"{Category}.Forbidden", "The used credentials don't have permission for the provided content id", HttpStatusCode.Forbidden);

        /// <summary>
        /// Represent a problem where the content uploaded has mature content tags, but the user doesn't have mature content enabled
        /// </summary>
        public static Problem MatureContentDisabled => new Problem($"{Category}.MatureContent.Disabled", "The used credentials have mature content disabled, while trying to upload mature content", HttpStatusCode.Forbidden);

        /// <summary>
        /// Represent a problem when a file uploaded is bigger than the limits set for it
        /// </summary>
        public static FileSizeProblem FileSizeTooLarge(string contentType, long uploadedSize, long maxSize) => new(contentType, uploadedSize, maxSize);

        /// <summary>
        /// Represent a problem when the provided file is invalid for what was requested
        /// </summary>
        public static InvalidFileTypeProblem InvalidFileType(string contentType, string providedType, IReadOnlyList<string> allowedTypes) => new(contentType, providedType, allowedTypes);

        /// <summary>
        /// Represent a problem when the provided file version is unsupported
        /// </summary>
        public static UnsupportedVersionProblem UnsupportedFileVersion(string fileId, string providedVersion, IReadOnlyList<string> allowedVersions) => new(fileId, providedVersion, allowedVersions);

        /// <summary>
        /// Represents a problem when a request is expecting certain entries in a payload, but they're missing
        /// </summary>
        public static MissingPayloadEntryProblem MissingPayloadEntry(IReadOnlyList<string> missingEntryPaths, string detail = null) => new(missingEntryPaths, detail);
    }
}
