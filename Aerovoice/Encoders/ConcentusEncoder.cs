using Aerovoice.Logging;
using Concentus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Aerovoice.Encoders
{
    public class ConcentusEncoder : IEncoder
    {
        private IOpusEncoder encoder = OpusCodecFactory.CreateEncoder(48000, 2);
        public byte[] Encode(byte[] data)
        {
            short[] pcmSamples = new short[data.Length / 2];
            Buffer.BlockCopy(data, 0, pcmSamples, 0, data.Length);

            // 20ms frame size, discord's default
            int frameSize = 48000 * 20 / 1000;

            byte[] encodedData = new byte[1275];

            ReadOnlySpan<short> pcmSpan = new ReadOnlySpan<short>(pcmSamples);
            Span<byte> encodedSpan = new Span<byte>(encodedData);

            int encodedLength = encoder.Encode(pcmSpan.Slice(0, frameSize * 2), frameSize, encodedSpan, encodedData.Length);
            Logger.Log($"Encoded {encodedLength} bytes out of {pcmSpan.Length} bytes!");
            return encodedData.Take(encodedLength).ToArray();
        }
    }
}
