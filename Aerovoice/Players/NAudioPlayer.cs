using System;
using System.Collections.Concurrent;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Aerovoice.Players
{
    public class NAudioPlayer : IPlayer
    {
        private readonly ConcurrentDictionary<uint, BufferedWaveProvider> _waveProviders = new();
        private readonly MixingSampleProvider _mixer;
        private readonly WaveOutEvent _waveOut;
        private readonly WaveFormat _format = new(48000, 16, 2);

        public NAudioPlayer()
        {
            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));
            _mixer.ReadFully = true;
            _waveOut = new WaveOutEvent();
            _waveOut.Init(_mixer);
            _waveOut.Play();
        }

        public void AddSamples(byte[] pcmData, int pcmLength, uint ssrc)
        {
            if (!_waveProviders.TryGetValue(ssrc, out var waveProvider))
            {
                waveProvider = new BufferedWaveProvider(_format)
                {
                    DiscardOnBufferOverflow = true
                };
                var sampleProvider = waveProvider.ToSampleProvider();
                _waveProviders.TryAdd(ssrc, waveProvider);
                _mixer.AddMixerInput(sampleProvider);
            }

            _waveProviders[ssrc].AddSamples(pcmData, 0, pcmLength);
        }
    }
}