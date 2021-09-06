using Core.Hashing;
using Relo;
using System;
using System.Runtime.InteropServices;

namespace AssetStream
{
    [StructLayout(LayoutKind.Sequential)]
    public struct AssetHandle : IComparable<AssetHandle>, IEquatable<AssetHandle>
    {
        public class Wrapper : AStructWrapper<AssetHandle>
        {
            public unsafe uint TypeId { get => Data->TypeId; set => Data->TypeId = value; }
            public unsafe uint InstanceId { get => Data->InstanceId; set => Data->InstanceId = value; }

            public override unsafe void InPlaceEndianToPlatform()
            {
                Tracker.ByteSwap32(&Data->TypeId);
                Tracker.ByteSwap32(&Data->InstanceId);
            }
        }

        public uint TypeId;
        public uint InstanceId;

        public static explicit operator AssetHandle(string x)
        {
            string[] typeAndId = x.Split(':');
            return new() { TypeId = HashProvider.GetCaseSenstitiveSymbolHash(typeAndId[0]), InstanceId = HashProvider.GetCaseInsenstitiveSymbolHash(typeAndId[1]) };
        }

        public int CompareTo(AssetHandle other)
        {
            return TypeId != other.TypeId
                ? TypeId > other.TypeId ? 1 : -1
                : InstanceId == other.InstanceId ? 0 : InstanceId > other.InstanceId ? 1 : -1;
        }

        public bool Equals(AssetHandle other)
        {
            return InstanceId == other.InstanceId && TypeId == other.TypeId;
        }

        public override bool Equals(object obj)
        {
            if (obj is Wrapper objWrapper)
            {
                return InstanceId == objWrapper.InstanceId && TypeId == objWrapper.TypeId;
            }
            return obj is AssetHandle objT && Equals(objT);
        }

        public override int GetHashCode()
        {
            return (int)InstanceId ^ (int)TypeId;
        }
    }
}
