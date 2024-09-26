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
        public byte[] Decode(byte[] data, int length, out int decodedLength, uint ssrc)
        {
            //var decoded = opusDecoders[ssrc].Decode(data, length, out decodedLength);
            //return decoded;
            var decoder = opusDecoders.GetOrAdd(ssrc, _ => new OpusDecoder(48000, 2));
            var decoded = decoder.Decode(data, length, out decodedLength);
            return decoded;
        }
    }
}
