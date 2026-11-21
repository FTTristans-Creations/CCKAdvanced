using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CVR.CCKEditor.Util
{
    /// <summary>
    /// A <see cref="Stream"/> wrapper that tracks the read progress of an underlying stream.
    /// </summary>
    /// <remarks>
    /// This stream is designed to wrap an existing <see cref="Stream"/> (like a file stream)
    /// and report progress via a callback during read operations. The progress is reported
    /// as a <see cref="long"/> value representing the current bytes of the stream that has
    /// been read. This is useful for displaying per-file upload progress when sending data
    /// over HTTP.
    /// </remarks>
    public class FileProgressStream : Stream
    {
        private readonly Stream _inner;
        private readonly Action<long> _progress;
        private long _bytesReadSoFar;
        
        public FileProgressStream(Stream inner, Action<long> progress)
        {
            _inner = inner;
            _progress = progress;
        }
        
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int bytesRead = await _inner.ReadAsync(buffer, cancellationToken);
            if (bytesRead > 0)
            {
                _bytesReadSoFar += bytesRead;
                _progress(_bytesReadSoFar);
            }
            return bytesRead;
        }
        
        // Required overrides
        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
    }
}