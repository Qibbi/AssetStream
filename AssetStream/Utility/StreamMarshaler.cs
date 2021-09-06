using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Utility
{
    public static class StreamMarshaler
    {
        private const int _bufferSize = 1024;

        private static byte[] _buffer;
        private static readonly List<byte> _charBuffer = new();
        private static readonly List<ushort> _charUnicodeBuffer = new();

        public static unsafe void Marshal(Stream stream, out uint objT)
        {
            byte[] buffer = _buffer;
            if (_buffer is null)
            {
                buffer = _buffer = new byte[_bufferSize];
            }
            int bytesRead = stream.Read(buffer, 0, sizeof(uint));
            if (bytesRead != sizeof(uint))
            {
                throw new InvalidOperationException("End of stream.");
            }

            fixed (byte* pBuffer = buffer)
            {
                objT = *(uint*)pBuffer;
            }
        }

        public static unsafe void Marshal(Stream stream, out int objT)
        {
            byte[] buffer = _buffer;
            if (_buffer is null)
            {
                buffer = _buffer = new byte[_bufferSize];
            }
            int bytesRead = stream.Read(buffer, 0, sizeof(int));
            if (bytesRead != sizeof(int))
            {
                throw new InvalidOperationException("End of stream.");
            }

            fixed (byte* pBuffer = buffer)
            {
                objT = *(int*)pBuffer;
            }
        }

        public static unsafe void Marshal<T>(Stream stream, out T objT) where T : unmanaged, Enum
        {
            byte[] buffer = _buffer;
            if (_buffer is null)
            {
                buffer = _buffer = new byte[_bufferSize];
            }
            int bytesRead = stream.Read(buffer, 0, sizeof(int));
            if (bytesRead != sizeof(int))
            {
                throw new InvalidOperationException("End of stream.");
            }

            fixed (byte* pBuffer = buffer)
            {
                objT = *(T*)pBuffer;
            }
        }

        public static unsafe void MarshalAnsiNullTerminated(Stream stream, out string objT)
        {
            int c;
            while ((c = stream.ReadByte()) != 0 && c != -1)
            {
                _charBuffer.Add((byte)c);
            }
            byte[] buffer = _charBuffer.ToArray();
            fixed (byte* pBuffer = buffer)
            {
                objT = new string((sbyte*)pBuffer, 0, buffer.Length, Encoding.ASCII);
            }
            _charBuffer.Clear();
        }

        public static unsafe void MarshalAnsi(Stream stream, out string objT)
        {
            Marshal(stream, out int length);
            byte[] buffer = _buffer;
            if (buffer.Length < length)
            {
                buffer = new byte[length];
            }
            int bytesRead = stream.Read(buffer, 0, length);
            if (bytesRead != length)
            {
                throw new InvalidOperationException("End of stream.");
            }

            fixed (byte* pBuffer = buffer)
            {
                objT = new string((sbyte*)pBuffer, 0, length, Encoding.ASCII);
            }
        }

        public static unsafe void MarshalUnicode(Stream stream, out string objT)
        {
            Marshal(stream, out int length);
            length <<= 1;
            byte[] buffer = _buffer;
            if (buffer.Length < length)
            {
                buffer = new byte[length];
            }
            int bytesRead = stream.Read(buffer, 0, length);
            if (bytesRead != length)
            {
                throw new InvalidOperationException("End of stream.");
            }

            fixed (byte* pBuffer = buffer)
            {
                objT = new string((sbyte*)pBuffer, 0, length, Encoding.Unicode);
            }
        }

        public static unsafe void MarshalUnicodeNegate(Stream stream, out string objT)
        {
            Marshal(stream, out int length);
            length <<= 1;
            byte[] buffer = _buffer;
            if (buffer.Length < length)
            {
                buffer = new byte[length];
            }
            int bytesRead = stream.Read(buffer, 0, length);
            if (bytesRead != length)
            {
                throw new InvalidOperationException("End of stream.");
            }

            fixed (byte* pBuffer = buffer)
            {
                byte* c = pBuffer;
                for (int idx = 0; idx < length; ++idx)
                {
                    *c = (byte)~*c;
                    ++c;
                }
                objT = new string((sbyte*)pBuffer, 0, length, Encoding.Unicode);
            }
        }
    }
}
