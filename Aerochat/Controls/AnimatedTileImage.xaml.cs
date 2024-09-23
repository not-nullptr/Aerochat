using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Timer = System.Timers.Timer;

namespace Aerochat.Controls
{
    /// <summary>
    /// Play an image from an animated sprite sheet.
    /// </summary>
    public partial class AnimatedTileImage : UserControl
    {
        private bool _paused = false;
        public AnimatedTileImage()
        {
            InitializeComponent();

            _timer = new Timer();
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = true;
            _timer.Start();
            Unloaded += AnimatedTileImage_Unloaded;

            SetupImageProperties();
            UpdateFrameRenderProperties();
        }

        public static readonly DependencyProperty FrameWidthProperty = DependencyProperty.Register("FrameWidth", typeof(int), typeof(AnimatedTileImage), new PropertyMetadata(0, OnChange));
        public static readonly DependencyProperty FrameHeightProperty = DependencyProperty.Register("FrameHeight", typeof(int), typeof(AnimatedTileImage), new PropertyMetadata(0, OnChange));
        public static readonly DependencyProperty FrameDurationProperty = DependencyProperty.Register("FrameDuration", typeof(int), typeof(AnimatedTileImage), new PropertyMetadata(0, OnChange));
        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register("Image", typeof(ImageSource), typeof(AnimatedTileImage), new PropertyMetadata(default(ImageSource), OnChange));
        public static readonly DependencyProperty LoopProperty = DependencyProperty.Register("Loop", typeof(bool), typeof(AnimatedTileImage), new PropertyMetadata(true, OnChange));

        private static void OnChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnimatedTileImage control && control._timer != null)
            {
                // if its 0 set it to 50 by default
                if (control.FrameDuration == 0)
                {
                    control.FrameDuration = 50;
                }
                if (e.Property == ImageProperty)
                {
                    control.Reset();
                }
                control._timer.Interval = control.FrameDuration;

                control.SetupImageProperties();
                control.InvalidateVisual();
            }
        }

        public int FrameWidth
        {
            get => (int)GetValue(FrameWidthProperty);
            set => SetValue(FrameWidthProperty, value);
        }

        public int FrameHeight
        {
            get => (int)GetValue(FrameHeightProperty);
            set => SetValue(FrameHeightProperty, value);
        }

        public int FrameDuration
        {
            get => (int)GetValue(FrameDurationProperty);
            set => SetValue(FrameDurationProperty, value);
        }

        public ImageSource? Image
        {
            get { return (ImageSource)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); InvalidateVisual(); }
        }

        public bool Loop
        {
            get => (bool)GetValue(LoopProperty);
            set => SetValue(LoopProperty, value);
        }

        private int _currentFrame = 0;

        public int CurrentFrame
        {
            get => _currentFrame;
            set
            {
                _currentFrame = value;
                UpdateFrameRenderProperties();
            }
        }

        private int _frameCount = 0;
        private Timer _timer;

        private bool SetupImageProperties()
        {
            if (Image != null &&
                Image is BitmapSource bitmapSource &&
                bitmapSource.PixelWidth != 0 &&
                bitmapSource.PixelHeight != 0 &&
                FrameWidth != 0 &&
                FrameHeight != 0
            )
            {
                _frameCount = (int)(bitmapSource.PixelWidth / FrameWidth);
                if (_frameCount == 0)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        private void AnimatedTileImage_Unloaded(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _timer.Elapsed -= Timer_Elapsed;
            _timer.Dispose();
            _timer = null;

            Unloaded -= AnimatedTileImage_Unloaded;

            // dispose of the image
            if (Image is BitmapSource bitmapSource)
            {
                bitmapSource.Freeze();
            }

            Image = null;

            // force a GC collection
            GC.Collect(2, GCCollectionMode.Forced, true, true);
        }

        private void UpdateFrameRenderProperties()
        {
            _imageElement.RenderTransform = new TranslateTransform(
                -1 * FrameWidth * CurrentFrame,
                0
            );
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Application.Current == null || _paused) return;
            if (_frameCount > 0)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Loop)
                    {
                        CurrentFrame = (CurrentFrame + 1) % _frameCount;
                    }
                    else
                    {
                        if (CurrentFrame < _frameCount - 1)
                        {
                            CurrentFrame++;
                        }
                    }
                });
            }
        }
        public void Reset()
        {
            CurrentFrame = 0;
            _timer.Start();
            _paused = false;
        }

        public void Pause()
        {
            _timer.Stop();
            _paused = true;
        }

        public void Play()
        {
            _timer.Start();
            _paused = false;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            return new Size(FrameWidth, FrameHeight);
        }
    }
}
