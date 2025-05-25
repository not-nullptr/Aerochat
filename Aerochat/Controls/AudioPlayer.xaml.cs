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
using Vanara.Extensions.Reflection;
using System.IO;

namespace Aerochat.Controls
{
    public partial class AudioPlayer : UserControl
    {

        private MediaPlayer mediaPlayer = new MediaPlayer();

        DispatcherTimer soundTimer = new DispatcherTimer();

        private double preMuteVolume = 0;
        private bool muted = false;

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

        public static readonly DependencyProperty VolumeStateProperty = DependencyProperty.Register(nameof(VolumeState),
            typeof(Volume),
            typeof(AudioPlayer),
            new PropertyMetadata(Volume.Medium));

        public Volume VolumeState
        {
            get => (Volume)GetValue(VolumeStateProperty);
            set => SetValue(VolumeStateProperty, value);
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
            InitializeComponent();;
            soundTimer.Interval = TimeSpan.FromMilliseconds(1);
            soundTimer.Tick += Timer_Tick;

            TimeSlider.Loaded += TimeSlider_Loaded;

            Unloaded += AudioPlayer_Unloaded;
        }

        private void AudioPlayer_Unloaded(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop();
            mediaPlayer.Close();
        }

        private void TimeSlider_Loaded(object sender, RoutedEventArgs e)
        {
            Thumb thumb = (Thumb)TimeSlider.Template.FindName("TimeThumb", TimeSlider);

            if (thumb != null)
            {
                thumb.DragStarted += OnDragStart;
                thumb.DragCompleted += OnDragEnd;
            }
        }

        private void TimeSlider_Changed(object sender, RoutedEventArgs e)
        {
            if (!mediaPlayer.NaturalDuration.HasTimeSpan) return;

            TimeSpan currentTime = TimeSpan.FromSeconds(TimeSlider.Value / 100 * mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds);

            TimeLabel.Content = ConvertTime(currentTime, mediaPlayer.NaturalDuration.TimeSpan);
        }

        private void VolumeButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (!muted)
            {
                preMuteVolume = mediaPlayer.Volume;
                VolumeSlider.Value = 0;
            }
            else
            {
                mediaPlayer.Volume = preMuteVolume;
                VolumeSlider.Value = preMuteVolume * 100;
                muted = true;
            }
            muted = !muted;
        }

        private void VolumeSlider_Changed(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Volume = VolumeSlider.Value / 100;
            if (VolumeSlider.Value == 0) {
                VolumeState = Volume.Muted;
            }
            if (VolumeSlider.Value > 0) {
                VolumeState = Volume.Low;
                if (muted) muted = false;
            }
            if (VolumeSlider.Value > 31)
            {
                VolumeState = Volume.Medium;
            }
            if (VolumeSlider.Value > 62)
            {
                VolumeState = Volume.High;
            }
        }

        private void OnDragEnd(object sender, DragCompletedEventArgs e)
        {
            mediaPlayer.Position = TimeSpan.FromMilliseconds(TimeSlider.Value / 100 * mediaPlayer.NaturalDuration.TimeSpan.TotalMilliseconds);
            if (Playing == PlayingState.Playing)
            {
                mediaPlayer.Play();
                soundTimer.Start();
            }
        }

        private void OnDragStart(object sender, DragStartedEventArgs e)
        {
            mediaPlayer.Pause();
            soundTimer.Stop();
        }

        // Forgive me father.
        private string ConvertTime(TimeSpan time, TimeSpan maxtime)
        {
            string convertedTime = "";

            var curTime = time;
            string processConversion()
            {
                var minutes = curTime.TotalMinutes < 10 ? $"0{(int)curTime.TotalMinutes}" : ((int)curTime.TotalMinutes).ToString();
                var seconds = curTime.TotalSeconds % 60 < 10 ? $"0{(int)(curTime.TotalSeconds % 60)}" : ((int)(curTime.TotalSeconds % 60)).ToString();
                return $"{minutes}:{seconds}";
            }
            convertedTime += processConversion() + "/";
            curTime = maxtime;
            convertedTime += processConversion();

            return convertedTime;
        }


        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!mediaPlayer.NaturalDuration.HasTimeSpan) return;
            TimeSpan currentPosition = mediaPlayer.Position;
            TimeSpan totalPosition = mediaPlayer.NaturalDuration.TimeSpan;
            TimeSlider.Value = currentPosition.TotalMilliseconds / mediaPlayer.NaturalDuration.TimeSpan.TotalMilliseconds * 100;
            
   

        }

        private void LoadSound(string url)
        {
            try
            {
                mediaPlayer.Open(new Uri(url, UriKind.Absolute));
                mediaPlayer.MediaEnded += OnSoundEnded;
                mediaPlayer.MediaOpened += OnSoundOpened;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error opening sound: " + ex.Message);
            }
        }

        private void OnSoundOpened(object? sender, EventArgs e)
        {
            PlayButton.Visibility = Visibility.Visible;
            PlayButton_Disabled.Visibility = Visibility.Collapsed;
            TimeLabel.Content = ConvertTime(TimeSpan.Zero, mediaPlayer.NaturalDuration.TimeSpan);
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
                await File.WriteAllBytesAsync(saveFile.FileName, bytes);
            }


        }


        public void OnPlayClick(object sender, RoutedEventArgs e)
        {
            if (Playing == PlayingState.Stopped)
            {
                if (mediaPlayer.Position == mediaPlayer.NaturalDuration.TimeSpan)
                { 
                    mediaPlayer.Position = TimeSpan.Zero;
                }
                mediaPlayer.Play();
                soundTimer.Start();
                Playing = PlayingState.Playing;
            }
            else if (Playing == PlayingState.Playing)
            {
                mediaPlayer.Pause();
                soundTimer.Stop();
                Playing = PlayingState.Paused;
            }
            else
            {
                mediaPlayer.Play();
                soundTimer.Start();
                Playing = PlayingState.Playing;
            }
        }

    }
}
