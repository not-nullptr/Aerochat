using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.Helpers
{
    public class TextToSpeech
    {
        public static TextToSpeech Instance { get; private set; }

        static TextToSpeech()
        {
            Instance = new();
        }

        private SpeechSynthesizer? _speechSynth = null;

        private TextToSpeech()
        {
            try
            {
                _speechSynth = new();
                _speechSynth.SetOutputToDefaultAudioDevice();
            }
            catch
            {
                // Ignore any exception creating the speech synthesiser; we don't care.
                _speechSynth = null;
            }
        }

        public bool Available
        {
            get => _speechSynth != null;
            private set { }
        }

        public void ReadOutMessage(string message)
        {
            _speechSynth?.SpeakAsync(message);
        }
    }
}
