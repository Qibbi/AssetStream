using Relo;
using System;
using System.Collections.Generic;
using System.IO;
using Utility;

namespace Core.IO
{
    public sealed class BigFile
    {
        public sealed class Header
        {
            private uint _type;
            private int _size;

            public uint Type { get => _type; private set => _type = value; }
            public int Size { get => _size; private set => _size = value; }
            public int NumberOfFiles { get; private set; }
            public int ManifestSize { get; private set; }

            public Header(Stream stream)
            {
                Marshal(stream, this);
            }

            private static unsafe void Marshal(Stream stream, Header objT)
            {
                StreamMarshaler.Marshal(stream, out objT._type);
                StreamMarshaler.Marshal(stream, out objT._size);
                StreamMarshaler.Marshal(stream, out int temp);
                Tracker.ByteSwap32((uint*)&temp);
                objT.NumberOfFiles = temp;
                StreamMarshaler.Marshal(stream, out temp);
                Tracker.ByteSwap32((uint*)&temp);
                objT.ManifestSize = temp;
            }

            public override string ToString()
            {
                return $"{Size} bytes, {NumberOfFiles} files";
            }
        }

        public sealed class Entry
        {
            private string _name;

            public int Offset { get; private set; }
            public int Size { get; private set; }
            public string Name { get => _name; private set => _name = value; }

            public Entry(Stream stream)
            {
                Marshal(stream, this);
            }

            private static unsafe void Marshal(Stream stream, Entry objT)
            {
                StreamMarshaler.Marshal(stream, out int temp);
                Tracker.ByteSwap32((uint*)&temp);
                objT.Offset = temp;
                StreamMarshaler.Marshal(stream, out temp);
                Tracker.ByteSwap32((uint*)&temp);
                objT.Size = temp;
                StreamMarshaler.MarshalAnsiNullTerminated(stream, out objT._name);
            }

            public override string ToString()
            {
                return $"{Name} @0x{Offset:X8}, {Size} bytes";
            }
        }

        private readonly string _path;
        private Header _header;

        public Dictionary<string, Entry> Files { get; } = new(StringComparer.OrdinalIgnoreCase);

        public BigFile(string path)
        {
            _path = path;
            Stream stream = File.Open(path, FileMode.Open);
            Marshal(stream, this);
        }

        private static void Marshal(Stream stream, BigFile objT)
        {
            objT._header = new(stream);
            for (int idx = 0; idx < objT._header.NumberOfFiles; ++idx)
            {
                Entry entry = new(stream);
                string name = entry.Name.Replace('/', '\\');
                if (!objT.Files.ContainsKey(name))
                {
                    objT.Files.Add(name, entry);
                }
            }
        }

        public bool Contains(string file)
        {
            return Files.ContainsKey(file);
        }

        public bool TryGet(string file, out Entry entry)
        {
            return Files.TryGetValue(file, out entry);
        }

        public Stream OpenStream(string file)
        {
            return TryGet(file, out Entry entry) ? OpenStream(entry) : null;
        }

        public Stream OpenStream(Entry file)
        {
            VirtualFileStream result = new(File.Open(_path, FileMode.Open, FileAccess.Read, FileShare.Read), file.Offset, file.Offset + file.Size);
            return RefPackFileStream.IsCompressed(result) ? new RefPackFileStream(result) : result;
        }

        public override string ToString()
        {
            return $"{_path} ({_header})";
        }
    }
}
