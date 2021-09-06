using Core;
using Core.IO;
using Relo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Utility;

namespace AssetStream
{
    public sealed class Manifest : ADisposeBase
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct ManifestHeader
        {
            public class Wrapper : AStructWrapper<ManifestHeader>
            {
                public unsafe bool IsBigEndian { get => Data->IsBigEndian; set => Data->IsBigEndian = value; }
                public unsafe bool IsLinked { get => Data->IsLinked; set => Data->IsLinked = value; }
                public unsafe ushort Version { get => Data->Version; set => Data->Version = value; }
                public unsafe uint StreamChecksum { get => Data->StreamChecksum; set => Data->StreamChecksum = value; }
                public unsafe uint AllTypesHash { get => Data->AllTypesHash; set => Data->AllTypesHash = value; }
                public unsafe int AssetCount { get => Data->AssetCount; set => Data->AssetCount = value; }
                public unsafe int TotalInstanceDataSize { get => Data->TotalInstanceDataSize; set => Data->TotalInstanceDataSize = value; }
                public unsafe int MaxInstanceChunkSize { get => Data->MaxInstanceChunkSize; set => Data->MaxInstanceChunkSize = value; }
                public unsafe int MaxRelocationChunkSize { get => Data->MaxRelocationChunkSize; set => Data->MaxRelocationChunkSize = value; }
                public unsafe int MaxImportsChunkSize { get => Data->MaxImportsChunkSize; set => Data->MaxImportsChunkSize = value; }
                public unsafe int AssetReferenceBufferSize { get => Data->AssetReferenceBufferSize; set => Data->AssetReferenceBufferSize = value; }
                public unsafe int ReferenceManifestNameBufferSize { get => Data->ReferenceManifestNameBufferSize; set => Data->ReferenceManifestNameBufferSize = value; }
                public unsafe int AssetNameBufferSize { get => Data->AssetNameBufferSize; set => Data->AssetNameBufferSize = value; }
                public unsafe int SourceFileNameBufferSize { get => Data->SourceFileNameBufferSize; set => Data->SourceFileNameBufferSize = value; }

                public override unsafe void InPlaceEndianToPlatform()
                {
                    Tracker.ByteSwap16(&Data->Version);
                    Tracker.ByteSwap32(&Data->StreamChecksum);
                    Tracker.ByteSwap32(&Data->AllTypesHash);
                    Tracker.ByteSwap32((uint*)&Data->AssetCount);
                    Tracker.ByteSwap32((uint*)&Data->TotalInstanceDataSize);
                    Tracker.ByteSwap32((uint*)&Data->MaxInstanceChunkSize);
                    Tracker.ByteSwap32((uint*)&Data->MaxRelocationChunkSize);
                    Tracker.ByteSwap32((uint*)&Data->MaxImportsChunkSize);
                    Tracker.ByteSwap32((uint*)&Data->AssetReferenceBufferSize);
                    Tracker.ByteSwap32((uint*)&Data->ReferenceManifestNameBufferSize);
                    Tracker.ByteSwap32((uint*)&Data->AssetNameBufferSize);
                    Tracker.ByteSwap32((uint*)&Data->SourceFileNameBufferSize);
                }
            }

            public SageBool IsBigEndian;
            public SageBool IsLinked;
            public ushort Version;
            public uint StreamChecksum;
            public uint AllTypesHash;
            public int AssetCount;
            public int TotalInstanceDataSize;
            public int MaxInstanceChunkSize;
            public int MaxRelocationChunkSize;
            public int MaxImportsChunkSize;
            public int AssetReferenceBufferSize;
            public int ReferenceManifestNameBufferSize;
            public int AssetNameBufferSize;
            public int SourceFileNameBufferSize;
        }

        private static readonly Dictionary<string, Manifest> _theAssetStreams = new(StringComparer.OrdinalIgnoreCase);

        private ManifestHeader.Wrapper _header;
        private Asset[] _assets;

        public Manifest BasePatchManifest { get; private set; }
        public Manifest[] ReferencedManifests { get; private set; }
        public IVirtualFileProvider FileProvider { get; private set; }
        public string FileName { get; private set; }
        public string BasePath { get; private set; }
        public bool IsBigEndian => _header.IsBigEndian;
        public bool IsLinked => _header.IsLinked;
        public ushort Version => _header.Version;
        public uint StreamChecksum => _header.StreamChecksum;
        public uint AllTypesHash => _header.AllTypesHash;
        public int AssetCount => _header.AssetCount;

        public Manifest(IVirtualFileProvider provider, string url, bool useVersionFile = true)
        {
            if (useVersionFile)
            {
                string basePath = VirtualFileSystem.Combine(VirtualFileSystem.GetParentFolder(url), VirtualFileSystem.GetFileNameWithoutExtension(url));
                string version = VirtualFileSystem.Combine("data", basePath + ".version");
                string streamVersion = string.Empty;
                if (provider.FileExists(version))
                {
                    using Stream versionStream = provider.OpenStream(version, FileMode.Open);
                    StreamMarshaler.MarshalAnsiNullTerminated(versionStream, out streamVersion);
                    streamVersion = streamVersion.Trim();
                }
                url = $"{basePath}{streamVersion}.manifest";
            }
            using Stream stream = provider.OpenStream(VirtualFileSystem.Combine("data", url), FileMode.Open);
            if (stream is null)
            {
                throw new FileNotFoundException($"Stream {url} not found in {provider}.");
            }
            Load(provider, url, stream);
        }

        private unsafe void Load(IVirtualFileProvider provider, string url, Stream stream)
        {
            FileProvider = provider;
            FileName = url;
            BasePath = VirtualFileSystem.Combine(VirtualFileSystem.GetParentFolder(url), VirtualFileSystem.GetFileNameWithoutExtension(url));
            _header = new ManifestHeader.Wrapper();
            _header.LoadFromStream(stream, false);
            if (IsBigEndian)
            {
                _header.InPlaceEndianToPlatform();
            }
            if (Version != 5)
            {
                throw new InvalidDataException("Can't read manifest. Unsupported file version.");
            }

            AssetEntry.Wrapper[] assetEntries = new AssetEntry.Wrapper[AssetCount];
            for (int idx = 0; idx < AssetCount; ++idx)
            {
                AssetEntry.Wrapper assetEntry = new();
                assetEntry.LoadFromStream(stream, IsBigEndian);
                assetEntries[idx] = assetEntry;
            }
            byte* assetReferenceBuffer = (byte*)Marshal.AllocHGlobal(_header.AssetReferenceBufferSize);
            if (stream.Read(new Span<byte>(assetReferenceBuffer, _header.AssetReferenceBufferSize)) != _header.AssetReferenceBufferSize)
            {
                throw new InvalidDataException("Can't read manifest. Unexpected end of stream.");
            }
            sbyte* referenceManifestNameBuffer = (sbyte*)Marshal.AllocHGlobal(_header.ReferenceManifestNameBufferSize);
            if (stream.Read(new Span<byte>(referenceManifestNameBuffer, _header.ReferenceManifestNameBufferSize)) != _header.ReferenceManifestNameBufferSize)
            {
                throw new InvalidDataException("Can't read manifest. Unexpected end of stream.");
            }
            List<Manifest> referencedManifests = new();
            for (sbyte* bufferPosition = referenceManifestNameBuffer; bufferPosition < referenceManifestNameBuffer + _header.ReferenceManifestNameBufferSize;)
            {
                sbyte type = *bufferPosition++;
                string referencedManifestName = new(bufferPosition);
                bufferPosition += referencedManifestName.Length + 1;
                if (type == 2)
                {
                    BasePatchManifest = BasePatchManifest is null
                        ? (new(provider, referencedManifestName, false))
                        : throw new InvalidDataException("A stream can't have multiple base patch streams.");
                }
                else
                {
                    referencedManifests.Add(_theAssetStreams[referencedManifestName]);
                }
            }
            ReferencedManifests = referencedManifests.ToArray();
            Marshal.FreeHGlobal((IntPtr)referenceManifestNameBuffer);
            sbyte* assetNameBuffer = (sbyte*)Marshal.AllocHGlobal(_header.AssetNameBufferSize);
            if (stream.Read(new Span<byte>(assetNameBuffer, _header.AssetNameBufferSize)) != _header.AssetNameBufferSize)
            {
                throw new InvalidDataException("Can't read manifest. Unexpected end of stream.");
            }
            sbyte* sourceFileNameBuffer = (sbyte*)Marshal.AllocHGlobal(_header.SourceFileNameBufferSize);
            if (stream.Read(new Span<byte>(sourceFileNameBuffer, _header.SourceFileNameBufferSize)) != _header.SourceFileNameBufferSize)
            {
                throw new InvalidDataException("Can't read manifest. Unexpected end of stream.");
            }
            int linkedInstanceDataOffset = 4;
            int linkedRelocationDataOffset = 4;
            int linkedImportsDataOffset = 4;
            _assets = new Asset[AssetCount];
            using (Stream assetReferenceStream = new UnmanagedMemoryStream(assetReferenceBuffer, _header.AssetReferenceBufferSize))
            {
                for (int idx = 0; idx < _assets.Length; ++idx)
                {
                    AssetEntry.Wrapper assetEntry = assetEntries[idx];
                    if (assetEntry.InstanceDataSize == 0)
                    {
                        BasePatchManifest.TryFindAsset(new AssetHandle() { TypeId = assetEntry.TypeId, InstanceId = assetEntry.InstanceId }, out Asset patchedAsset);
                        _assets[idx] = patchedAsset;
                        assetReferenceStream.Seek(sizeof(AssetHandle) * assetEntry.AssetReferenceCount, SeekOrigin.Current);
                        continue;
                    }
                    AssetHandle[] references = new AssetHandle[assetEntry.AssetReferenceCount];
                    for (int reference = 0; reference < assetEntry.AssetReferenceCount; ++reference)
                    {
                        AssetHandle.Wrapper handleWrapper = new();
                        handleWrapper.LoadFromStream(assetReferenceStream, IsBigEndian);
                        references[reference] = *handleWrapper.Data;
                    }
                    Asset asset = new(assetEntry,
                                      new string(assetNameBuffer + assetEntry.NameOffset),
                                      new string(sourceFileNameBuffer + assetEntry.SourceFileNameOffset),
                                      references,
                                      linkedInstanceDataOffset,
                                      linkedRelocationDataOffset,
                                      linkedImportsDataOffset,
                                      this);
                    linkedInstanceDataOffset += assetEntry.InstanceDataSize;
                    linkedRelocationDataOffset += assetEntry.RelocationDataSize;
                    linkedImportsDataOffset += assetEntry.ImportsDataSize;
                    _assets[idx] = asset;
                }
            }
            Marshal.FreeHGlobal((IntPtr)sourceFileNameBuffer);
            Marshal.FreeHGlobal((IntPtr)assetNameBuffer);
            Marshal.FreeHGlobal((IntPtr)assetReferenceBuffer);
        }

        private bool TryFindAsset(AssetHandle assetHandle, out Asset asset)
        {
            if (!IsLinked)
            {
                throw new NotImplementedException();
            }
            foreach (Asset streamAsset in _assets)
            {
                if (streamAsset.Equals(assetHandle))
                {
                    asset = streamAsset;
                    return true;
                }
            }
            foreach (Manifest referencedManifest in ReferencedManifests)
            {
                if (referencedManifest.TryFindAsset(assetHandle, out asset))
                {
                    return true;
                }
            }
            asset = null;
            return false;
        }

        private Chunk GetAssetChunk(Asset asset)
        {
            Chunk result = new(asset.InstanceDataSize, asset.RelocationsDataSize, asset.ImportsDataSize);
            Stream instanceDataStream;
            Stream relocationDataStream;
            Stream importsDataStream;
            if (IsLinked)
            {
                instanceDataStream = FileProvider.OpenStream(VirtualFileSystem.Combine("data", BasePath + ".bin"), FileMode.Open);
                instanceDataStream.Seek(asset.LinkedInstanceDataOffset, SeekOrigin.Current);
                relocationDataStream = FileProvider.OpenStream(VirtualFileSystem.Combine("data", BasePath + ".relo"), FileMode.Open);
                relocationDataStream.Seek(asset.LinkedRelocationsDataOffset, SeekOrigin.Current);
                importsDataStream = FileProvider.OpenStream(VirtualFileSystem.Combine("data", BasePath + ".imp"), FileMode.Open);
                importsDataStream.Seek(asset.LinkedImportsDataOffset, SeekOrigin.Current);
            }
            else
            {
                throw new NotImplementedException();
            }
            if (instanceDataStream.Read(result.InstanceBuffer, 0, result.InstanceBuffer.Length) != result.InstanceBuffer.Length)
            {
                throw new InvalidDataException("Can't read asset instance data. Unexpected end of stream.");
            }
            if (relocationDataStream.Read(result.RelocationBuffer, 0, result.RelocationBuffer.Length) != result.RelocationBuffer.Length)
            {
                throw new InvalidDataException("Can't read asset relocation data. Unexpected end of stream.");
            }
            if (importsDataStream.Read(result.ImportsBuffer, 0, result.ImportsBuffer.Length) != result.ImportsBuffer.Length)
            {
                throw new InvalidDataException("Can't read asset imports data. Unexpected end of stream.");
            }
            instanceDataStream.Dispose();
            relocationDataStream.Dispose();
            importsDataStream.Dispose();
            return result;
        }

        private byte[] GetAssetCData(Asset asset)
        {
            Stream cdataStream = FileProvider.OpenStream(VirtualFileSystem.Combine("data", BasePath, "cdata", asset.BasePath + ".cdata"), FileMode.Open);
            cdataStream.Seek(16, SeekOrigin.Current);
            byte[] result = new byte[cdataStream.Length - 16];
            return cdataStream.Read(result, 0, result.Length) != result.Length
                ? throw new InvalidDataException("Can't read asset custom data. Unexpected end of stream.")
                : result;
        }

        public bool TryGetAssetChunk(AssetHandle assetHandle, out Chunk chunk)
        {
            if (TryFindAsset(assetHandle, out Asset asset))
            {
                chunk = asset.Manifest.GetAssetChunk(asset);
                return true;
            }
            chunk = null;
            return false;
        }

        public byte[] GetAssetCData(AssetHandle assetHandle)
        {
            return TryFindAsset(assetHandle, out Asset asset) ? asset.Manifest.GetAssetCData(asset) : null;
        }

        public override string ToString()
        {
            return $"{FileName} [0x{StreamChecksum:X8}, {AssetCount} assets]";
        }
    }
}
