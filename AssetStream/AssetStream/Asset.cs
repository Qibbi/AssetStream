using System;

namespace AssetStream
{
    public class Asset : IEquatable<AssetHandle>
    {
        public uint TypeId { get; }
        public uint InstanceId { get; }
        public uint TypeHash { get; }
        public uint InstanceHash { get; }
        public string BasePath { get; }
        public string AssetName { get; }
        public string TypeName => AssetName.Split(':')[0];
        public string InstanceName => AssetName.Split(':')[1];
        public string SourceFile { get; }
        public AssetHandle[] References { get; }
        public int InstanceDataSize { get; }
        public int RelocationsDataSize { get; }
        public int ImportsDataSize { get; }
        public int LinkedInstanceDataOffset { get; }
        public int LinkedRelocationsDataOffset { get; }
        public int LinkedImportsDataOffset { get; }
        public Manifest Manifest { get; }

        public Asset(AssetEntry.Wrapper assetEntry,
                     string name,
                     string source,
                     AssetHandle[] references,
                     int linkedInstanceOffset,
                     int linkedRelocationOffset,
                     int linkedImportsOffset,
                     Manifest manifest)
        {
            TypeId = assetEntry.TypeId;
            InstanceId = assetEntry.InstanceId;
            TypeHash = assetEntry.TypeHash;
            InstanceHash = assetEntry.InstanceHash;
            BasePath = $"{assetEntry.TypeId:x8}.{assetEntry.TypeHash:x8}.{assetEntry.InstanceId:x8}.{assetEntry.InstanceHash:x8}";
            AssetName = name;
            SourceFile = source;
            References = references;
            InstanceDataSize = assetEntry.InstanceDataSize;
            RelocationsDataSize = assetEntry.RelocationDataSize;
            ImportsDataSize = assetEntry.ImportsDataSize;
            LinkedInstanceDataOffset = linkedInstanceOffset;
            LinkedRelocationsDataOffset = linkedRelocationOffset;
            LinkedImportsDataOffset = linkedImportsOffset;
            Manifest = manifest;
        }

        public bool Equals(AssetHandle other)
        {
            return TypeId == other.TypeId && InstanceId == other.InstanceId;
        }

        public override string ToString()
        {
            return AssetName;
        }
    }
}
