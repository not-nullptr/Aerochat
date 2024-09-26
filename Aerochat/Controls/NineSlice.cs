using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Aerochat.Controls
{
    public class NineSlice : Control
    {
        private static readonly Action EmptyDelegate = delegate { };
        public ImageSource? Image
        {
            get { return (ImageSource)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); InvalidateVisual(); }
        }

        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register("Image", typeof(ImageSource), typeof(NineSlice), new PropertyMetadata(default(ImageSource), OnImageChanged));

        private static void OnImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NineSlice control)
            {
                control.InvalidateVisual();
            }
        }

        public Size Slice
        {
            get { return (Size)GetValue(SliceProperty); }
            set { SetValue(SliceProperty, value); InvalidateVisual(); }
        }

        public static readonly DependencyProperty SliceProperty =
            DependencyProperty.Register("Slice", typeof(Size), typeof(NineSlice), new PropertyMetadata(new Size() { Width = 8, Height = 8 }, OnSliceChanged));

        private static void OnSliceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NineSlice control)
            {
                control.InvalidateVisual();
            }
        }

        private void DrawImage(DrawingContext context, BitmapSource image, Int32Rect sourceRect, Rect destRect)
        {
            CroppedBitmap croppedBitmap = new CroppedBitmap(image, sourceRect);
            context.DrawImage(croppedBitmap, destRect);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (Image == null || !(Image is BitmapSource bitmapSource) || bitmapSource.PixelWidth == 0 || bitmapSource.PixelHeight == 0)
            {
                // clear the control if the image is null or empty
                drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));
                return;
            }
            DrawImage(drawingContext, bitmapSource, new Int32Rect(0, 0, (int)Math.Max(Slice.Width, 0), (int)Math.Max(Slice.Height, 0)), new Rect(0, 0, Math.Max(Slice.Width, 0), Math.Max(Slice.Height, 0)));
            DrawImage(drawingContext, bitmapSource, new Int32Rect((int)Math.Max(Slice.Width, 0), 0, (int)Math.Max(Image.Width - Slice.Width * 2, 0), (int)Math.Max(Slice.Height, 0)), new Rect(Math.Max(Slice.Width, 0), 0, Math.Max(ActualWidth - Slice.Width * 2, 0), Math.Max(Slice.Height, 0)));
            DrawImage(drawingContext, bitmapSource, new Int32Rect((int)Math.Max(Image.Width - Slice.Width, 0), 0, (int)Math.Max(Slice.Width, 0), (int)Math.Max(Slice.Height, 0)), new Rect(Math.Max(ActualWidth - Slice.Width, 0), 0, Math.Max(Slice.Width, 0), Math.Max(Slice.Height, 0)));
            DrawImage(drawingContext, bitmapSource, new Int32Rect(0, (int)Math.Max(Slice.Height, 0), (int)Math.Max(Slice.Width, 0), (int)Math.Max(Image.Height - Slice.Height * 2, 0)), new Rect(0, Math.Max(Slice.Height, 0), Math.Max(Slice.Width, 0), Math.Max(ActualHeight - Slice.Height * 2, 0)));
            DrawImage(drawingContext, bitmapSource, new Int32Rect((int)Math.Max(Slice.Width, 0), (int)Math.Max(Slice.Height, 0), (int)Math.Max(Image.Width - Slice.Width * 2, 0), (int)Math.Max(Image.Height - Slice.Height * 2, 0)), new Rect(Math.Max(Slice.Width, 0), Math.Max(Slice.Height, 0), Math.Max(ActualWidth - Slice.Width * 2, 0), Math.Max(ActualHeight - Slice.Height * 2, 0)));
            DrawImage(drawingContext, bitmapSource, new Int32Rect((int)Math.Max(Image.Width - Slice.Width, 0), (int)Math.Max(Slice.Height, 0), (int)Math.Max(Slice.Width, 0), (int)Math.Max(Image.Height - Slice.Height * 2, 0)), new Rect(Math.Max(ActualWidth - Slice.Width, 0), Math.Max(Slice.Height, 0), Math.Max(Slice.Width, 0), Math.Max(ActualHeight - Slice.Height * 2, 0)));
            DrawImage(drawingContext, bitmapSource, new Int32Rect(0, (int)Math.Max(Image.Height - Slice.Height, 0), (int)Math.Max(Slice.Width, 0), (int)Math.Max(Slice.Height, 0)), new Rect(0, Math.Max(ActualHeight - Slice.Height, 0), Math.Max(Slice.Width, 0), Math.Max(Slice.Height, 0)));
            DrawImage(drawingContext, bitmapSource, new Int32Rect((int)Math.Max(Slice.Width, 0), (int)Math.Max(Image.Height - Slice.Height, 0), (int)Math.Max(Image.Width - Slice.Width * 2, 0), (int)Math.Max(Slice.Height, 0)), new Rect(Math.Max(Slice.Width, 0), Math.Max(ActualHeight - Slice.Height, 0), Math.Max(ActualWidth - Slice.Width * 2, 0), Math.Max(Slice.Height, 0)));
            DrawImage(drawingContext, bitmapSource, new Int32Rect((int)Math.Max(Image.Width - Slice.Width, 0), (int)Math.Max(Image.Height - Slice.Height, 0), (int)Math.Max(Slice.Width, 0), (int)Math.Max(Slice.Height, 0)), new Rect(Math.Max(ActualWidth - Slice.Width, 0), Math.Max(ActualHeight - Slice.Height, 0), Math.Max(Slice.Width, 0), Math.Max(Slice.Height, 0)));
        }
    }
}
