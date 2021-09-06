using System;
using System.IO;

namespace Core.IO
{
    public class RefPackFileStream : VirtualFileStream
    {
        // good value for speed 8388608 Bytes (8192 KB (8 MB))
        private const int _preBufferLength = 0x00800000;
        // 0x00020000 actually 131072 Bytes (128 KB), 256 KB so it doesn't wrap around as much
        private const int _refBufferLength = 0x00040000;
        private const int _refBufferLengthMinusOne = 0x0003FFFF;

        private readonly int _flags;
        private readonly int _highestNonStopValue;
        private readonly int _length;
        private readonly int _dataOffset;
        private readonly byte[] _preBuffer;
        private readonly byte[] _refBuffer;

        private int _preBufferPosition;
        private int _refPosition;
        private long _position;

        public override long Length => _length;
        public override long Position
        {
            get => _position;
            set
            {
                if (value < _position)
                {
                    InternalStream.Seek(_dataOffset, SeekOrigin.Begin);
                }
                Seek(value, SeekOrigin.Begin);
            }
        }
        public override bool CanWrite => false;

        public RefPackFileStream(Stream internalStream, long startPosition = 0, long endPosition = -1, bool shouldDisposeInternalStream = true, bool shouldSeekToStart = true)
            : base(internalStream, startPosition, endPosition, shouldDisposeInternalStream, shouldSeekToStart)
        {
            _flags = InternalStream.ReadByte();
            _highestNonStopValue = InternalStream.ReadByte();
            _length = InternalStream.ReadByte();
            _length <<= 8;
            _length |= InternalStream.ReadByte();
            _length <<= 8;
            _length |= InternalStream.ReadByte();
            _dataOffset = 5;
            if ((_flags & 0x80) != 0)
            {
                _length <<= 8;
                _length |= InternalStream.ReadByte();
                _dataOffset = 6;
            }
            _preBuffer = new byte[_preBufferLength];
            InternalStream.Read(_preBuffer, 0, _preBuffer.Length);
            _preBufferPosition = 0;
            _refBuffer = new byte[_refBufferLength];
            _refPosition = 0;
            _position = 0;
        }

        public static bool IsCompressed(Stream stream)
        {
            if (stream is RefPackFileStream)
            {
                return true;
            }
            int x = stream.ReadByte();
            int y = stream.ReadByte();
            stream.Seek(-2L, SeekOrigin.Current);
            return (x & 0x3E) == 0x10 && y == 0xFB; // we currently only support 0xFB as highest non-stop value
        }

        private long Unpack(ref bool isEndOfFile)
        {
            byte first;
            byte second;
            byte third;
            byte fourth;
            int run;
            int refPosition;
            long result;
            // Prebuffer, 0x70 equals highest literal copy run
            if (_preBufferLength - _preBufferPosition < 0x74)
            {
                InternalStream.Position -= _preBufferLength - _preBufferPosition;
                InternalStream.Read(_preBuffer, 0, _preBuffer.Length);
                _preBufferPosition = 0;
            }
            first = _preBuffer[_preBufferPosition++];
            // ref0 = xyyzzzww where x = 0; y = offset decompressed bytes; z = count decompressed bytes; w = count new bytes
            // ref1 = yyyyyyyy
            if ((first & 0x80) == 0)
            {
                second = _preBuffer[_preBufferPosition++];
                // count range: 0x00 - 0x03 (0 - 3)
                run = first & 0x03;
                result = run;
                while (run-- > 0)
                {
                    _refBuffer[_refPosition++ & _refBufferLengthMinusOne] = _preBuffer[_preBufferPosition++];
                }
                // set refPosition to last decompressed byte minus range 0 - 0x03FF (0 - 1023)
                refPosition = _refPosition - 1 - (((first & 0x60) << 3) | second);
                // count range: 0x03 - 0x0A (3 - 10)
                run = ((first & 0x1C) >> 2) + 3;
                result += run;
                while (run-- > 0)
                {
                    _refBuffer[_refPosition++ & _refBufferLengthMinusOne] = _refBuffer[refPosition++ & _refBufferLengthMinusOne];
                }
            }
            // ref0 = xxzzzzzz where x = 10; y = offset decompressed bytes; z = count decompressed bytes; w = count new bytes
            // ref1 = wwyyyyyy
            // ref2 = yyyyyyyy
            else if ((first & 0x40) == 0)
            {
                second = _preBuffer[_preBufferPosition++];
                third = _preBuffer[_preBufferPosition++];
                // count range: 0x00 - 0x03 (0 - 3)
                run = second >> 6;
                result = run;
                while (run-- > 0)
                {
                    _refBuffer[_refPosition++ & _refBufferLengthMinusOne] = _preBuffer[_preBufferPosition++];
                }
                // set refPosition to last decompressed byte minus range 0 - 0x3FFF (0 - 16383)
                refPosition = _refPosition - 1 - (((second & 0x3F) << 8) | third);
                // count range 0x04 - 0x43 (4 - 67)
                run = (first & 0x3F) + 4;
                result += run;
                while (run-- > 0)
                {
                    _refBuffer[_refPosition++ & _refBufferLengthMinusOne] = _refBuffer[refPosition++ & _refBufferLengthMinusOne];
                }
            }
            // ref0 = xxxyzzww where x = 110; y = offset decompressed bytes; z = count decompressed bytes; w = count new bytes
            // ref1 = yyyyyyyy
            // ref2 = yyyyyyyy
            // ref3 = zzzzzzzz
            else if ((first & 0x20) == 0)
            {
                second = _preBuffer[_preBufferPosition++];
                third = _preBuffer[_preBufferPosition++];
                fourth = _preBuffer[_preBufferPosition++];
                // count range: 0x00 - 0x03 (0 - 3)
                run = first & 0x03;
                result = run;
                while (run-- > 0)
                {
                    _refBuffer[_refPosition++ & _refBufferLengthMinusOne] = _preBuffer[_preBufferPosition++];
                }
                // set refPosition to last decompressed byte minus range 0 - 0x01FFFF (0 - 131071)
                refPosition = _refPosition - 1 - ((((first & 0x10) >> 4) << 16) | (second << 8) | third);
                // count range 0x05 - 0x03FF (5 - 1028)
                run = (((first & 0x0C) >> 2) << 8) + fourth + 5;
                result += run;
                while (run-- > 0)
                {
                    _refBuffer[_refPosition++ & _refBufferLengthMinusOne] = _refBuffer[refPosition++ & _refBufferLengthMinusOne];
                }
            }
            // ref0 = xxxwwwww where x = 111; w = count new bytes;
            // 0xFB Highest Non Stop Value; equals if (count <= 0x70)
            else if (first <= _highestNonStopValue)
            {
                // count range 4 - 0x80 (4 - 128)
                // actual range 4 - 0x70 (4 - 112), always multiple of 4
                run = ((first & 0x1F) + 1) << 2;
                result = run;
                while (run-- > 0)
                {
                    _refBuffer[_refPosition++ & _refBufferLengthMinusOne] = _preBuffer[_preBufferPosition++];
                }

            }
            // end of file
            // count range 0 - 0x03 (0 - 3)
            else
            {
                // count range: 0x00 - 0x03 (0 - 3)
                run = first & 0x03;
                result = run;
                while (run-- > 0)
                {
                    _refBuffer[_refPosition++ & _refBufferLengthMinusOne] = _preBuffer[_preBufferPosition++];
                }
                isEndOfFile = true;
            }
            return result;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Current:
                    offset += _position;
                    break;
                case SeekOrigin.End:
                    offset = _length - offset;
                    break;
            }
            if (offset == _position)
            {
                return _position;
            }
            if (offset < _position)
            {
                InternalStream.Seek(_dataOffset, SeekOrigin.Begin);
                InternalStream.Read(_preBuffer, 0, _preBuffer.Length);
                _preBufferPosition = 0;
                _refPosition = 0;
                _position = 0;
            }
            long copyRun;
            long copyPart;
            long result = 0L;
            bool isEndOfFile = false;
            while (!isEndOfFile && _position < offset)
            {
                if (_position < _refPosition)
                {
                    copyRun = _refPosition - _position;
                }
                else
                {
                    copyRun = Unpack(ref isEndOfFile);
                }
                if (copyRun > offset - result)
                {
                    copyRun = offset - result;
                }
                if (((_position & _refBufferLengthMinusOne) + copyRun) > _refBufferLength)
                {
                    copyPart = _refBufferLength - (_position & _refBufferLengthMinusOne);
                    result += copyPart;
                    _position += copyPart;
                    copyRun -= copyPart;
                }
                result += copyRun;
                _position += copyRun;
            }
            return result;
        }

        public override int ReadByte()
        {
            byte[] b = new byte[1];
            int length = Read(b, 0, 1);
            return length == 1 ? b[0] : -1;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count == 0)
            {
                return 0;
            }
            long copyRun;
            long copyPart;
            int result = 0;
            bool isEndOfFile = false;
            while (!isEndOfFile && result < count)
            {
                copyRun = _position < _refPosition ? _refPosition - _position : Unpack(ref isEndOfFile);
                if (copyRun > count - result)
                {
                    copyRun = count - result;
                }
                if (((_position & _refBufferLengthMinusOne) + copyRun) > _refBufferLength)
                {
                    copyPart = _refBufferLength - (_position & _refBufferLengthMinusOne);
                    Array.Copy(_refBuffer, _position & _refBufferLengthMinusOne, buffer, offset, copyPart);
                    offset += (int)copyPart;
                    result += (int)copyPart;
                    _position += copyPart;
                    copyRun -= copyPart;
                }
                Array.Copy(_refBuffer, _position & _refBufferLengthMinusOne, buffer, offset, copyRun);
                offset += (int)copyRun;
                result += (int)copyRun;
                _position += copyRun;
            }
            return result;
        }

        public override void WriteByte(byte value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
