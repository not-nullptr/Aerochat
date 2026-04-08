using System;
using System.Collections.Concurrent;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Aerovoice.Players
{
    public class NAudioPlayer : IPlayer
    {
        private readonly ConcurrentDictionary<uint, BufferedWaveProvider> _waveProviders = new();
        private readonly ConcurrentDictionary<uint, VolumeSampleProvider> _volumeProviders = new();
        private readonly ConcurrentDictionary<uint, bool> _mutedSsrcs = new();
        private readonly MixingSampleProvider _mixer;
        private readonly WaveOutEvent _waveOut;
        private readonly WaveFormat _format = new(48000, 16, 2);

        public float ClientTransmitVolume { get; set; } = 1.0f;

        public NAudioPlayer()
        {
            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));
            _mixer.ReadFully = true;
            _waveOut = new WaveOutEvent();
            _waveOut.Init(_mixer);
            _waveOut.Play();
        }

        public void SetSsrcVolume(uint ssrc, float volume)
        {
            if (_volumeProviders.TryGetValue(ssrc, out var vp))
                vp.Volume = volume;
        }

        public void SetSsrcMuted(uint ssrc, bool muted)
        {
            _mutedSsrcs[ssrc] = muted;
        }

        public bool IsSsrcMuted(uint ssrc)
        {
            return _mutedSsrcs.TryGetValue(ssrc, out var muted) && muted;
        }

        public float GetSsrcVolume(uint ssrc)
        {
            return _volumeProviders.TryGetValue(ssrc, out var vp) ? vp.Volume : 1.0f;
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
                var volumeProvider = new VolumeSampleProvider(sampleProvider);
                _waveProviders.TryAdd(ssrc, waveProvider);
                _volumeProviders.TryAdd(ssrc, volumeProvider);
                _mixer.AddMixerInput(volumeProvider);
            }

            _waveProviders[ssrc].AddSamples(pcmData, 0, pcmLength);
        }
    }
}
