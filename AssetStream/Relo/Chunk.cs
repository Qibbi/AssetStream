using System;

namespace Relo
{
    public sealed class Chunk
    {
        public byte[] InstanceBuffer { get; private set; }
        public byte[] RelocationBuffer { get; private set; }
        public byte[] ImportsBuffer { get; private set; }

        public Chunk()
        {
            InstanceBuffer = Array.Empty<byte>();
            RelocationBuffer = Array.Empty<byte>();
            ImportsBuffer = Array.Empty<byte>();
        }

        public Chunk(int instanceDataSize, int relocationsDataSize, int importsDataSize)
        {
            InstanceBuffer = new byte[instanceDataSize];
            RelocationBuffer = new byte[relocationsDataSize];
            ImportsBuffer = new byte[importsDataSize];
        }

        internal bool Allocate(uint instanceBufferSize, uint relocationBufferSize, uint importsBufferSize)
        {
            InstanceBuffer = new byte[instanceBufferSize];
            if (relocationBufferSize > 0)
            {
                RelocationBuffer = new byte[relocationBufferSize];
            }
            if (importsBufferSize > 0)
            {
                ImportsBuffer = new byte[importsBufferSize];
            }
            return true;
        }
    }
}
