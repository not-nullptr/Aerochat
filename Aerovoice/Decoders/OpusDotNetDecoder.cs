using OpusDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerovoice.Decoders
{
    public class OpusDotNetDecoder : IDecoder
    {
        private OpusDecoder opusDecoder = new(48000, 2);
        public byte[] Decode(byte[] data, int length, out int decodedLength)
        {
            var decoded = opusDecoder.Decode(data, length, out decodedLength);
            return decoded;
        }
    }
}
