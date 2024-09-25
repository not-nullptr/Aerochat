using Sodium;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerovoice.Decryptors
{
    public class AEADAES256GCMRTPSize : BaseDecryptor
    {
        public static new string Name => "aead_aes256_gcm_rtpsize";

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

            var nonce = new byte[12];
            Array.Copy(opusSpan, opusSpan.Length - 4, nonce, 0, 4);
            // remove nonce from data
            opusSpan = opusSpan.Take(opusSpan.Length - 4).ToArray();
            var result = SecretAeadAes.Decrypt(opusSpan, nonce, key, header);
            return isExtended ? result.Skip(len * 4).ToArray() : result;
        }
    }
}
