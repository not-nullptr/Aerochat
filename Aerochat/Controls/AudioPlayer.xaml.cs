using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Aerochat.ViewModels;
using Aerochat.Enums;
using Vanara.PInvoke;
using System.Windows.Threading;
using System.Reactive;
using System.Windows.Controls.Primitives;
using System.Net;
using System.Net.Http;

namespace Aerochat.Controls
{
    public partial class AudioPlayer : UserControl
    {

        private MediaPlayer _mediaPlayer = new MediaPlayer();

        DispatcherTimer soundTimer = new DispatcherTimer();




        public static readonly DependencyProperty UrlProperty = DependencyProperty.Register(nameof(Url), 
            typeof(string), 
            typeof(AudioPlayer), 
            new PropertyMetadata(null, OnUrlChanged));
      
        public string Url
        {
            get => (string)GetValue(UrlProperty);
            set => SetValue(UrlProperty, value);
        }

        public static readonly DependencyProperty NameProperty = DependencyProperty.Register(nameof(Name),
            typeof(string),
            typeof(AudioPlayer),
            new PropertyMetadata(null, OnUrlChanged));

        public string Name
        {
            get => (string)GetValue(NameProperty);
            set => SetValue(NameProperty, value);
        }

        public static readonly DependencyProperty PlayingProperty = DependencyProperty.Register(nameof(Playing),
            typeof(PlayingState),
            typeof(AudioPlayer),
            new PropertyMetadata(PlayingState.Stopped));

        public PlayingState Playing
        {
            get => (PlayingState)GetValue(PlayingProperty);
            set => SetValue(PlayingProperty, value);
        }


        private static void OnUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d != null)
            {
               var audioplayer = (AudioPlayer)d;
               audioplayer.LoadSound((string)d.GetValue(UrlProperty));
            }
        }

        public AudioPlayer()
        {
            InitializeComponent();
            Visibility = Visibility.Collapsed;
            soundTimer.Interval = TimeSpan.FromMilliseconds(1);
            soundTimer.Tick += Timer_Tick;

            TimeSlider.Loaded += Slider_Loaded;
        }

        private void Slider_Loaded(object sender, RoutedEventArgs e)
        {
            Thumb thumb = (Thumb)TimeSlider.Template.FindName("TimeThumb", TimeSlider);

            if (thumb != null)
            {
                thumb.DragStarted += OnDragStart;
                thumb.DragCompleted += OnDragEnd;
            }
        }

        private void OnDragEnd(object sender, DragCompletedEventArgs e)
        {
            _mediaPlayer.Position = TimeSpan.FromMilliseconds(TimeSlider.Value / 100 * _mediaPlayer.NaturalDuration.TimeSpan.TotalMilliseconds);
            if (Playing == PlayingState.Playing)
            {
                _mediaPlayer.Play();
                soundTimer.Start();
            }
        }

        private void OnDragStart(object sender, DragStartedEventArgs e)
        {
            _mediaPlayer.Pause();
            soundTimer.Stop();
        }


        private void Timer_Tick(object sender, EventArgs e)
        {
            TimeSpan currentPosition = _mediaPlayer.Position;
            TimeSlider.Value = currentPosition.TotalMilliseconds / _mediaPlayer.NaturalDuration.TimeSpan.TotalMilliseconds * 100;
            _mediaPlayer.Volume = VolumeSlider.Value / 100;
        }

        private void LoadSound(string url)
        {
            try
            {
                _mediaPlayer.Open(new Uri(url, UriKind.Absolute));
                _mediaPlayer.MediaEnded += OnSoundEnded;
                _mediaPlayer.MediaOpened += OnSoundOpened;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error opening sound: " + ex.Message);
            }
        }

        private void OnSoundOpened(object? sender, EventArgs e)
        {
            Visibility = Visibility.Visible;


        }

        private void OnSoundEnded(object sender, EventArgs e)
        {
            Playing = PlayingState.Stopped;
        }


        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog saveFile = new System.Windows.Forms.SaveFileDialog
            {
                Title = "Save audio file",
                Filter = "MP3 files (*.mp3)|*.mp3|WAV files (*.wav)|*.wav|OGG files (*.ogg)|*.ogg|FLAC files (*.flac)|*.flac|All files (*.*)|*.*",
                FileName = Name
            };

            if (saveFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                HttpClient client = new HttpClient();
                byte[] bytes = await client.GetByteArrayAsync(Url);
            }


        }


        public void OnPlayClick(object sender, RoutedEventArgs e)
        {
            if (Playing == PlayingState.Stopped)
            {
                if (_mediaPlayer.Position == _mediaPlayer.NaturalDuration.TimeSpan)
                { 
                    _mediaPlayer.Position = TimeSpan.Zero;
                }
                _mediaPlayer.Play();
                soundTimer.Start();
                Playing = PlayingState.Playing;
            }
            else if (Playing == PlayingState.Playing)
            {
                _mediaPlayer.Pause();
                soundTimer.Stop();
                Playing = PlayingState.Paused;
            }
            else
            {
                _mediaPlayer.Play();
                soundTimer.Start();
                Playing = PlayingState.Playing;
            }
        }

    }
}
