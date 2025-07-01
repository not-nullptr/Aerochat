using Aerochat.Controls.NineSliceStuff;
using NAudio.CoreAudioApi;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Aerochat.Controls
{
    /// <summary>
    /// A standard nine-slice image background control. The corners of the image are kept proportional, and the centre of
    /// the image is stretched to fill its container.
    /// </summary>
    public class NineSlice : Control
    {
        protected NineSliceImageSet? _imageSet = null;

        public ImageSource? Image
        {
            get { return (ImageSource)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); InvalidateVisual(); }
        }

        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register("Image", typeof(ImageSource), typeof(NineSlice), new PropertyMetadata(default(ImageSource), OnImageChanged));

        protected static void OnImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NineSlice control)
            {
                control.ReloadImage();
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
                control.ReloadImage();
                control.InvalidateVisual();
            }
        }

        // Virtual so that child classes can modify the implementation to fit their needs.
        protected virtual void ReloadImage()
        {
            _imageSet = null;

            if (Image == null || !(Image is BitmapSource bitmapSource) || bitmapSource.PixelWidth == 0 || bitmapSource.PixelHeight == 0)
            {
                return;
            }

            _imageSet = NineSliceCacheManager.Instance.FindOrCreateImageSet(bitmapSource, Slice);
        }

        // Common static method for NineSliceButton's sake.
        protected static CroppedBitmap? ResolvePartBitmapFromImageSet(NineSliceImageSet? imageSet, NineSlicePart part)
        {
            return part switch
            {
                NineSlicePart.TopLeft => imageSet?.TopLeft,
                NineSlicePart.TopCenter => imageSet?.TopCenter,
                NineSlicePart.TopRight => imageSet?.TopRight,
                NineSlicePart.CenterLeft => imageSet?.CenterLeft,
                NineSlicePart.CenterCenter => imageSet?.CenterCenter,
                NineSlicePart.CenterRight => imageSet?.CenterRight,
                NineSlicePart.BottomLeft => imageSet?.BottomLeft,
                NineSlicePart.BottomCenter => imageSet?.BottomCenter,
                NineSlicePart.BottomRight => imageSet?.BottomRight,

                _ => throw new NotImplementedException("What the hell??"), // Shut up linter
            };
        }

        // Virtual so that child classes can modify the implementation to fit their needs.
        protected virtual CroppedBitmap? ResolvePartBitmap(NineSlicePart part)
        {
            return ResolvePartBitmapFromImageSet(_imageSet, part);
        }

        protected void DrawImage(DrawingContext context, NineSlicePart part, Rect destRect)
        {
            CroppedBitmap? targetBitmap = ResolvePartBitmap(part);

            if (targetBitmap != null)
            {
                context.DrawImage(targetBitmap, destRect);
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // For NineSliceButton: Instead of using the Image property to make sure that the image is not transparent,
            // we'll just test that we have a top-left fragment. If we do, then we certainly have other fragments as
            // well. If we were to test Image, then this wouldn't work for nine-slice buttons where the default state
            // is transparent (no image) but there is another state.
            CroppedBitmap? testingFragment = ResolvePartBitmap(NineSlicePart.TopLeft);

            if (testingFragment == null || !(testingFragment is BitmapSource bitmapSource) || bitmapSource.PixelWidth == 0 || bitmapSource.PixelHeight == 0)
            {
                // clear the control if the image is null or empty
                drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));
                return;
            }

            DrawImage(drawingContext, NineSlicePart.TopLeft, new Rect(0, 0, Math.Max(Slice.Width, 0), Math.Max(Slice.Height, 0)));
            DrawImage(drawingContext, NineSlicePart.TopCenter, new Rect(Math.Max(Slice.Width, 0), 0, Math.Max(ActualWidth - Slice.Width * 2, 0), Math.Max(Slice.Height, 0)));
            DrawImage(drawingContext, NineSlicePart.TopRight, new Rect(Math.Max(ActualWidth - Slice.Width, 0), 0, Math.Max(Slice.Width, 0), Math.Max(Slice.Height, 0)));
            DrawImage(drawingContext, NineSlicePart.CenterLeft, new Rect(0, Math.Max(Slice.Height, 0), Math.Max(Slice.Width, 0), Math.Max(ActualHeight - Slice.Height * 2, 0)));
            DrawImage(drawingContext, NineSlicePart.CenterCenter, new Rect(Math.Max(Slice.Width, 0), Math.Max(Slice.Height, 0), Math.Max(ActualWidth - Slice.Width * 2, 0), Math.Max(ActualHeight - Slice.Height * 2, 0)));
            DrawImage(drawingContext, NineSlicePart.CenterRight, new Rect(Math.Max(ActualWidth - Slice.Width, 0), Math.Max(Slice.Height, 0), Math.Max(Slice.Width, 0), Math.Max(ActualHeight - Slice.Height * 2, 0)));
            DrawImage(drawingContext, NineSlicePart.BottomLeft, new Rect(0, Math.Max(ActualHeight - Slice.Height, 0), Math.Max(Slice.Width, 0), Math.Max(Slice.Height, 0)));
            DrawImage(drawingContext, NineSlicePart.BottomCenter, new Rect(Math.Max(Slice.Width, 0), Math.Max(ActualHeight - Slice.Height, 0), Math.Max(ActualWidth - Slice.Width * 2, 0), Math.Max(Slice.Height, 0)));
            DrawImage(drawingContext, NineSlicePart.BottomRight, new Rect(Math.Max(ActualWidth - Slice.Width, 0), Math.Max(ActualHeight - Slice.Height, 0), Math.Max(Slice.Width, 0), Math.Max(Slice.Height, 0)));
        }
    }
}
