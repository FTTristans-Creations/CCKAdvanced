using System.Net;

namespace CVR.CCKEditor.ContentUploader.ContentUploaderModels.Problems
{
    /// <summary>
    /// Represent a problem when a file uploaded is bigger than the limits set for it
    /// </summary>
    public sealed class FileSizeProblem : Problem
    {
        /// <summary>
        /// File type
        /// </summary>
        public string FileType { get; set; }

        /// <summary>
        /// Uploaded size in bytes
        /// </summary>
        public long UploadedSize { get; set; }

        /// <summary>
        /// Maximum size allowed in bytes
        /// </summary>
        public long MaxSize { get; set; }

        /// <summary>
        /// Constructor for the problem
        /// </summary>
        public FileSizeProblem(string fileType, long uploadedSize, long maxSize) : base(
            $"{UploadError.Category}.File.TooLarge",
            $"The supplied file is too large for {fileType}. You tried to upload {uploadedSize} bytes, but the maximum size is {maxSize} bytes.",
            HttpStatusCode.RequestEntityTooLarge)
        {
            FileType = fileType;
            UploadedSize = uploadedSize;
            MaxSize = maxSize;
        }
    }
}