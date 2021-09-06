using System;
using System.IO;

namespace Core.IO
{
    public class VirtualFileStream : Stream
    {
        private readonly bool _shouldDisposeInternalStream;

        public Stream InternalStream { get; protected set; }
        public long StartPosition { get; }
        public long EndPosition { get; }
        public override bool CanRead => InternalStream.CanRead;
        public override bool CanSeek => InternalStream.CanSeek;
        public override bool CanWrite => InternalStream.CanWrite;
        public override long Length => EndPosition == -1L ? InternalStream.Length - StartPosition : EndPosition - StartPosition;
        public override long Position { get => InternalStream.Position - StartPosition; set => InternalStream.Position = StartPosition + value; }

        public VirtualFileStream(Stream internalStream, long startPosition = 0L, long endPosition = -1L, bool shouldDisposeInternalStream = true, bool shouldSeekToStart = true)
        {
            if (startPosition < 0L)
            {
                throw new ArgumentOutOfRangeException($"{nameof(startPosition)} cannot be negative.");
            }

            _shouldDisposeInternalStream = shouldDisposeInternalStream;
            if (internalStream is VirtualFileStream stream)
            {
                internalStream = stream.InternalStream;
                startPosition += stream.StartPosition;
                endPosition = endPosition == -1L ? stream.EndPosition : endPosition + stream.StartPosition;
            }
            InternalStream = internalStream;
            StartPosition = startPosition;
            EndPosition = endPosition;
            if (shouldSeekToStart)
            {
                InternalStream.Seek(startPosition, SeekOrigin.Begin);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_shouldDisposeInternalStream && InternalStream is not null)
            {
                InternalStream.Dispose();
                InternalStream = null;
            }
            base.Dispose(disposing);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    InternalStream.Seek(StartPosition + offset, SeekOrigin.Begin);
                    break;
                case SeekOrigin.Current:
                    InternalStream.Seek(offset, SeekOrigin.Current);
                    break;
                case SeekOrigin.End:
                    if (EndPosition == -1L)
                    {
                        InternalStream.Seek(offset, SeekOrigin.End);
                    }
                    else
                    {
                        InternalStream.Seek(EndPosition - StartPosition + offset, SeekOrigin.Begin);
                    }
                    break;
            }
            long position = InternalStream.Position;
            if (position < StartPosition || (EndPosition != -1L && position > EndPosition))
            {
                InternalStream.Position = StartPosition;
                throw new InvalidOperationException("Seeked out of bounds.");
            }
            return position;
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException("Can't resize stream.");
        }

        public override int ReadByte()
        {
            return EndPosition != -1L && InternalStream.Position >= EndPosition ? -1 : InternalStream.ReadByte();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (EndPosition == -1L)
            {
                int max = (int)(EndPosition - InternalStream.Position);
                if (count > max)
                {
                    count = max;
                }
            }
            return InternalStream.Read(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            if (EndPosition != -1L && 1 > EndPosition - InternalStream.Position)
            {
                throw new NotSupportedException("Can't resize stream.");
            }

            InternalStream.WriteByte(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (EndPosition != -1L && count > EndPosition - InternalStream.Position)
            {
                throw new NotSupportedException("Can't resize stream.");
            }

            InternalStream.Write(buffer, offset, count);
        }

        public override void Flush()
        {
            InternalStream.Flush();
        }
    }
}
