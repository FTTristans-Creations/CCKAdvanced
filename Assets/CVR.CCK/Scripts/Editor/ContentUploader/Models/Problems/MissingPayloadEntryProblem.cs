using System.Collections.Generic;
using System.Net;

namespace CVR.CCKEditor.ContentUploader.ContentUploaderModels.Problems
{
    /// <summary>
    /// Represents a problem when a request is expecting certain entries in a payload, but they're missing
    /// </summary>
    public sealed class MissingPayloadEntryProblem : Problem
    {
        /// <summary>
        /// Missing entries paths
        /// Eg: Files.AssetBundle
        /// </summary>
        public IReadOnlyList<string> ExpectedEntries { get; set; }

        /// <summary>
        /// Constructor for the problem
        /// </summary>
        /// <param name="expectedEntries">paths of the missing entries</param>
        /// <param name="detail">An optional extra detail to append to the error</param>
        public MissingPayloadEntryProblem(IReadOnlyList<string> expectedEntries, string? detail = null) : base(
            $"{UploadError.Category}.File.MissingEntry",
            $"There are missing entries in the payload.{(detail == null ? string.Empty : $" {detail}")}",
            HttpStatusCode.BadRequest)
        {
            ExpectedEntries = expectedEntries;
        }
    }
}