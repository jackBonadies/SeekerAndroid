using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    /// <summary>
    /// Wraps a write-only stream (e.g. Android OutputStreamInvoker) to track Position,
    /// which the Slsk.net library reads for download progress reporting.
    /// </summary>
    public class PositionTrackingOutputStream : Stream
    {
        private readonly Stream _inner;
        private long _position;

        public PositionTrackingOutputStream(Stream inner, long initialPosition = 0)
        {
            _inner = inner;
            _position = initialPosition;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _inner.Write(buffer, offset, count);
            _position += count;
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _inner.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            _position += count;
        }

        public override void Flush() => _inner.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
