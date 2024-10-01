using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerovoice.Decoders
{
    public interface IDecoder
    {
        public byte[] Decode(byte[] data, int length, out int decodedLength, uint ssrc, ushort increment);
    }
}
