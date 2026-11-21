using System.Collections.Generic;
using System.Net;

namespace CVR.CCKEditor.ContentUploader.ContentUploaderModels.Problems
{
    /// <summary>
    /// Represent a problem when the provided file is invalid for what was requested
    /// </summary>
    public sealed class InvalidFileTypeProblem : Problem
    {
        /// <summary>
        /// The identifier of the content we're having a problem with
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// The provided type of the content
        /// </summary>
        public string ProvidedType { get; set; }

        /// <summary>
        /// The allowed content types
        /// </summary>
        public IReadOnlyList<string> ExpectedTypes { get; set; }

        /// <summary>
        /// Constructor of the problem
        /// </summary>
        public InvalidFileTypeProblem(string contentType, string providedType, IReadOnlyList<string> expectedTypes)
            : base($"{UploadError.Category}.File.InvalidType",
                "The supplied file type is invalid.",
                HttpStatusCode.UnsupportedMediaType)
        {
            ContentType = contentType;
            ProvidedType = providedType;
            ExpectedTypes = expectedTypes;
        }
    }
}