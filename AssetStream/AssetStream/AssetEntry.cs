using Relo;
using System.Runtime.InteropServices;

namespace AssetStream
{
    [StructLayout(LayoutKind.Sequential)]
    public struct AssetEntry
    {
        public class Wrapper : AStructWrapper<AssetEntry>
        {
            public unsafe uint TypeId { get => Data->TypeId; set => Data->TypeId = value; }
            public unsafe uint InstanceId { get => Data->InstanceId; set => Data->InstanceId = value; }
            public unsafe uint TypeHash { get => Data->TypeHash; set => Data->TypeHash = value; }
            public unsafe uint InstanceHash { get => Data->InstanceHash; set => Data->InstanceHash = value; }
            public unsafe int AssetReferenceOffset { get => Data->AssetReferenceOffset; set => Data->AssetReferenceOffset = value; }
            public unsafe int AssetReferenceCount { get => Data->AssetReferenceCount; set => Data->AssetReferenceCount = value; }
            public unsafe int NameOffset { get => Data->NameOffset; set => Data->NameOffset = value; }
            public unsafe int SourceFileNameOffset { get => Data->SourceFileNameOffset; set => Data->SourceFileNameOffset = value; }
            public unsafe int InstanceDataSize { get => Data->InstanceDataSize; set => Data->InstanceDataSize = value; }
            public unsafe int RelocationDataSize { get => Data->RelocationDataSize; set => Data->RelocationDataSize = value; }
            public unsafe int ImportsDataSize { get => Data->ImportsDataSize; set => Data->ImportsDataSize = value; }

            public override unsafe void InPlaceEndianToPlatform()
            {
                Tracker.ByteSwap32(&Data->TypeId);
                Tracker.ByteSwap32(&Data->InstanceId);
                Tracker.ByteSwap32(&Data->TypeHash);
                Tracker.ByteSwap32(&Data->InstanceHash);
                Tracker.ByteSwap32((uint*)&Data->AssetReferenceOffset);
                Tracker.ByteSwap32((uint*)&Data->AssetReferenceCount);
                Tracker.ByteSwap32((uint*)&Data->NameOffset);
                Tracker.ByteSwap32((uint*)&Data->SourceFileNameOffset);
                Tracker.ByteSwap32((uint*)&Data->InstanceDataSize);
                Tracker.ByteSwap32((uint*)&Data->RelocationDataSize);
                Tracker.ByteSwap32((uint*)&Data->ImportsDataSize);
            }
        }

        public uint TypeId;
        public uint InstanceId;
        public uint TypeHash;
        public uint InstanceHash;
        public int AssetReferenceOffset;
        public int AssetReferenceCount;
        public int NameOffset;
        public int SourceFileNameOffset;
        public int InstanceDataSize;
        public int RelocationDataSize;
        public int ImportsDataSize;
    }
}
