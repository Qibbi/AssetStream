using System;
using System.Xml;

namespace Core.Hashing
{
    public static class HashProvider
    {
        public static uint GetTypeHash(Type type)
        {
            return GetTypeHash((uint)type.FullName.Length, type);
        }

        public static uint GetTypeHash(uint combine, Type type)
        {
            return FastHash.GetHashCode(FastHash.GetHashCode(combine, type.FullName), type.Module.ModuleVersionId.ToByteArray());
        }

        public static uint GetCaseInsenstitiveSymbolHash(string symbol)
        {
            return FastHash.GetHashCode(symbol.ToLower());
        }

        public static uint GetCaseSenstitiveSymbolHash(string symbol)
        {
            return FastHash.GetHashCode(symbol);
        }

        public static uint GetTextHash(string text)
        {
            return FastHash.GetHashCode(text);
        }

        public static uint GetTextHash(uint hash, string text)
        {
            return FastHash.GetHashCode(hash, text);
        }

        public static uint GetDataHash(byte[] data)
        {
            return FastHash.GetHashCode(data);
        }

        public static uint GetDataHash(uint hash, byte[] data)
        {
            return FastHash.GetHashCode(hash, data);
        }

        public static uint GetXmlHash(uint hash, XmlNode node)
        {
            HashingWriter writer = new HashingWriter(hash);
            XmlWriter xmlWriter = XmlWriter.Create(writer);
            node.WriteTo(xmlWriter);
            xmlWriter.Flush();
            writer.Flush();
            return writer.GetFinalHash();
        }

        public static uint GetXmlHash(XmlNode node)
        {
            return GetXmlHash(0u, node);
        }
    }
}
