using Native;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AssetStream
{
    public abstract class AStructWrapper<T> : IDisposable where T : unmanaged
    {
        public static unsafe int Size { get; } = sizeof(T);

        private unsafe T* _data;

        protected IntPtr HData;

        public unsafe T* Data { get => _data; protected set => _data = value; }

        public unsafe AStructWrapper()
        {
            HData = Marshal.AllocHGlobal(Size);
            if (HData != IntPtr.Zero)
            {
                MsVcRt.MemSet(HData, 0, Size);
                Data = (T*)HData;
            }
        }

        public abstract void InPlaceEndianToPlatform();

        public virtual unsafe void LoadFromBuffer(byte[] buffer, bool isBigEndian)
        {
            fixed (byte* pBuffer = &buffer[0])
            {
                MsVcRt.MemCpy((IntPtr)Data, (IntPtr)pBuffer, Size);
                if (isBigEndian)
                {
                    InPlaceEndianToPlatform();
                }
            }
        }

        public virtual unsafe void LoadFromStream(Stream input, bool isBigEndian)
        {
            if (input.Read(new Span<byte>(Data, Size)) != Size)
            {
                throw new InvalidDataException($"{typeof(T).Name} cannot be read from the stream.");
            }
            if (isBigEndian)
            {
                InPlaceEndianToPlatform();
            }
        }

        public virtual unsafe void SaveToStream(Stream output, bool isBigEndian)
        {
            if (isBigEndian)
            {
                InPlaceEndianToPlatform();
            }
            byte[] buffer = new byte[Size];
            new UnmanagedMemoryStream((byte*)Data, Size).Read(buffer, 0, Size);
            output.Write(buffer, 0, Size);
            if (isBigEndian)
            {
                InPlaceEndianToPlatform();
            }
        }

        public virtual unsafe void Dispose()
        {
            if (HData != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(HData);
                HData = IntPtr.Zero;
                Data = null;
                GC.SuppressFinalize(this);
            }
        }
    }
}
