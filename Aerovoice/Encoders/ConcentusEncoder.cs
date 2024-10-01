using Concentus;
using Concentus.Oggfile;


namespace Aerovoice.Encoders
{
    public class ConcentusEncoder : IEncoder
    {
        private IOpusEncoder encoder = OpusCodecFactory.CreateEncoder(48000, 2);
        private OpusOggWriteStream oggWriter;
        private FileStream outputStream = new("output.ogg", FileMode.Create);

        public ConcentusEncoder()
        {
            encoder.UseInbandFEC = true;
            oggWriter = new OpusOggWriteStream(encoder, outputStream);
        }

        public byte[] Encode(byte[] data)
        {
            short[] pcmSamples = new short[data.Length / 2];
            Buffer.BlockCopy(data, 0, pcmSamples, 0, data.Length);

            int frameSize = 48000 * 20 / 1000;
            byte[] encodedData = new byte[1275];

            ReadOnlySpan<short> pcmSpan = new ReadOnlySpan<short>(pcmSamples);
            Span<byte> encodedSpan = new Span<byte>(encodedData);

            int encodedLength = encoder.Encode(pcmSpan.Slice(0, frameSize * 2), frameSize, encodedSpan, encodedData.Length);
            var encodedFrame = encodedData.Take(encodedLength).ToArray();

            // Write the encoded frame to the Ogg file
            return encodedFrame;
        }

        public void Dispose()
        {
            oggWriter.Finish();
            encoder.Dispose();
        }
    }


}
