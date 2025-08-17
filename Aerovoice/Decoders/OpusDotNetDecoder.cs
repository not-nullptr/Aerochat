using OpusDotNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerovoice.Decoders
{
    public class OpusDotNetDecoder : IDecoder
    {
        private readonly ConcurrentDictionary<uint, OpusDecoder> opusDecoders = new();
        private readonly ConcurrentDictionary<uint, int> lastIncrements = new();
        public byte[] Decode(byte[] data, int length, out int decodedLength, uint ssrc, ushort increment)
        {
            // if there's no increment, set it to increment
            if (!lastIncrements.TryGetValue(ssrc, out var lastIncrement))
            {
                lastIncrements.TryAdd(ssrc, increment);
                lastIncrement = increment;
            }
            var decoder = opusDecoders.GetOrAdd(ssrc, _ => new OpusDecoder(20, 48000, 2)
            {
                FEC = true,
            });
            byte[] decoded = Array.Empty<byte>();
            if (lastIncrement + 1 != increment)
            {
                decoded = decoder.Decode(null, -1, out decodedLength);
            }
            else
            {
                decoded = decoder.Decode(data, length, out decodedLength);
            }
            lastIncrements[ssrc] = increment;
            return decoded;
        }
    }
}
