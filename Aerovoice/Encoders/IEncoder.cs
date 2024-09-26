using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerovoice.Encoders
{
    public interface IEncoder
    {
        public byte[] Encode(byte[] data);
    }
}
