using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Aerochat.Controls
{
    public class TileImage : Control
    {
        public static readonly DependencyProperty FrameWidthProperty = DependencyProperty.Register("FrameWidth", typeof(int), typeof(TileImage), new PropertyMetadata(0, OnChange));
        public static readonly DependencyProperty FrameHeightProperty = DependencyProperty.Register("FrameHeight", typeof(int), typeof(TileImage), new PropertyMetadata(0, OnChange));
        public static readonly DependencyProperty FrameDurationProperty = DependencyProperty.Register("FrameDuration", typeof(int), typeof(TileImage), new PropertyMetadata(0, OnChange));
        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register("Image", typeof(ImageSource), typeof(TileImage), new PropertyMetadata(default(ImageSource), OnChange));
        public static readonly DependencyProperty LoopProperty = DependencyProperty.Register("Loop", typeof(bool), typeof(TileImage), new PropertyMetadata(true, OnChange));

        private static void OnChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TileImage control && control._timer != null)
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
        private int _frameCount = 0;
        private Timer _timer;

        private void DrawImage(DrawingContext context, BitmapSource image, Int32Rect sourceRect, Rect destRect)
        {
            CroppedBitmap croppedBitmap = new CroppedBitmap(image, sourceRect);
            context.DrawImage(croppedBitmap, destRect);
        }

        public TileImage()
        {
            _timer = new Timer();
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = true;
            _timer.Start();
            Unloaded += TileImage_Unloaded;
        }

        private void TileImage_Unloaded(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _timer.Elapsed -= Timer_Elapsed;
            _timer.Dispose();
            _timer = null;

            Unloaded -= TileImage_Unloaded;

            // dispose of the image
            if (Image is BitmapSource bitmapSource)
            {
                bitmapSource.Freeze();
            }

            Image = null;

            // force a GC collection
            GC.Collect(2, GCCollectionMode.Forced, true, true);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (Image == null || !(Image is BitmapSource bitmapSource) || bitmapSource.PixelWidth == 0 || bitmapSource.PixelHeight == 0)
            {
                drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));
                return;
            }

            _frameCount = (int)(bitmapSource.PixelWidth / FrameWidth);
            if (_frameCount == 0)
            {
                return;
            }

            DrawImage(drawingContext, bitmapSource, new Int32Rect(_currentFrame * FrameWidth, 0, FrameWidth, FrameHeight), new Rect(0, 0, FrameWidth, FrameHeight));
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Application.Current == null) return;
            if (_frameCount > 0)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Loop)
                    {
                        _currentFrame = (_currentFrame + 1) % _frameCount;
                    }
                    else
                    {
                        if (_currentFrame < _frameCount - 1)
                        {
                            _currentFrame++;
                        }
                    }

                    InvalidateVisual();
                });
            }
        }
        public void Reset()
        {
            _currentFrame = 0;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            return new Size(FrameWidth, FrameHeight);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            return new Size(FrameWidth, FrameHeight);
        }
    }
}
