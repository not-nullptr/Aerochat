using System;
using System.IO;
using System.Windows.Media;

namespace Aerochat.Helpers
{
    public static class SoundHelper
    {
        private static readonly string SoundBase = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Resources", "Sounds");

        private static MediaPlayer _sfxPlayer;

        public static Uri GetSoundUri(string fileName)
        {
            return new Uri(Path.Combine(SoundBase, fileName), UriKind.Absolute);
        }

        public static void PlaySound(string fileName)
        {
            if (_sfxPlayer == null)
            {
                _sfxPlayer = new MediaPlayer();
                _sfxPlayer.MediaOpened += (s, e) => _sfxPlayer.Play();
            }
            _sfxPlayer.Open(GetSoundUri(fileName));
        }
    }
}
