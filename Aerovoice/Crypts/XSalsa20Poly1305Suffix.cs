using Sodium;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerovoice.Crypts
{
    public class XSalsa20Poly1305Suffix : BaseCrypt
    {
        public static new string Name => "xsalsa20_poly1305_suffix";

        public override byte[] Decrypt(byte[] data, byte[] key)
        {
            var type = data[0];
            var toSkip = 1 + 1 + 2 + 4 + 4;
            bool isExtended = data[toSkip] == 0xbe && data[toSkip + 1] == 0xde;
            int len = 0;
            if (isExtended)
            {
                toSkip += 2;
                len = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(toSkip));
                toSkip += 2;
            }
            var header = data.Take(toSkip).ToArray();
            var opusSpan = data.Skip(toSkip).ToArray();

            var nonce = new byte[24];
            // 24 random bytes, apparently
            Array.Copy(opusSpan, opusSpan.Length - 24, nonce, 0, 4);
            // remove nonce from data
            opusSpan = opusSpan.Take(opusSpan.Length - 24).ToArray();

            var result = SecretBox.Open(opusSpan, nonce, key);
            return isExtended ? result.Skip(len * 4).ToArray() : result;
        }
        public override byte[] Encrypt(byte[] data, byte[] key)
        {
            throw new NotImplementedException();
        }
    }
}
