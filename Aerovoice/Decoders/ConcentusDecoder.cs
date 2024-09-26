using Concentus;
using Concentus.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Aerovoice.Decoders
{
    public class ConcentusDecoder : IDecoder
    {
        private IOpusDecoder opusDecoder = OpusCodecFactory.CreateDecoder(48000, 2);
        public byte[] Decode(byte[] data, int length, out int decodedLength, uint ssrc)
        {
            Span<byte> encoded = data.AsSpan(0, length);
            Span<short> decoded = stackalloc short[
                OpusPacketInfo.GetNumSamplesPerFrame(encoded, 48000) * 2
            ];
            decodedLength = opusDecoder.Decode(encoded, decoded, 5760);
            Span<byte> bytes = MemoryMarshal.AsBytes(decoded);
            return bytes.ToArray();
        }
    }
}
