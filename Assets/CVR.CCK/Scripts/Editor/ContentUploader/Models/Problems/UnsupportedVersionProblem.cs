using System.Collections.Generic;
using System.Net;

namespace CVR.CCKEditor.ContentUploader.ContentUploaderModels.Problems
{
    /// <summary>
    /// Represents a problem when a file is provided with an unsupported version
    /// </summary>
    public sealed class UnsupportedVersionProblem : Problem
    {
        /// <summary>
        /// Allowed versions
        /// Eg: 2021.3.X
        /// </summary>
        public IReadOnlyList<string> AllowedVersions { get; set; }

        /// <summary>
        /// File identifier that has the problem
        /// Eg: Files.AssetBundle
        /// </summary>
        public string FileId { get; set; }

        /// <summary>
        /// Version of the file with the problem
        /// Eg: 2019.1.20
        /// </summary>
        public string ProvidedVersion { get; set; }

        /// <summary>
        /// Constructor for the problem
        /// </summary>
        /// <param name="fileId">file identifier that has the problem</param>
        /// <param name="allowedVersions">versions that we support</param>
        /// <param name="providedVersion">version that was provided</param>
        public UnsupportedVersionProblem(string fileId, string? providedVersion, IReadOnlyList<string> allowedVersions) : base(
            $"{UploadError.Category}.File.UnsupportedVersion",
            $"The version {providedVersion ?? "<null>"} is not supported for {fileId}. Current supported versions: {string.Join(", ", allowedVersions)}",
            HttpStatusCode.BadRequest)
        {
            FileId = fileId;
            ProvidedVersion = providedVersion;
            AllowedVersions = allowedVersions;
        }
    }
}