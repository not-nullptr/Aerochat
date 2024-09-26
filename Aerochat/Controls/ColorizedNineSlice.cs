using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Color = System.Windows.Media.Color;

namespace Aerochat.Controls
{
    public class ColorizedNineSlice : NineSlice
    {
        public static readonly DependencyProperty TintColorProperty =
            DependencyProperty.Register(nameof(TintColor), typeof(Color), typeof(ColorizedNineSlice),
            new FrameworkPropertyMetadata(Colors.Red, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DarkenProperty =
            DependencyProperty.Register(nameof(Darken), typeof(double?), typeof(ColorizedNineSlice),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, null, CoerceDarken));

        public static readonly DependencyProperty BackgroundProperty =
    DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(ColorizedNineSlice),
    new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }
        public Color TintColor
        {
            get { return (Color)GetValue(TintColorProperty); }
            set { SetValue(TintColorProperty, value); }
        }

        // Darken factor property (0 to 1), allowing null
        public double? Darken
        {
            get { return (double?)GetValue(DarkenProperty); }
            set { SetValue(DarkenProperty, value); }
        }

        // Ensure Darken is between 0 and 1
        private static object CoerceDarken(DependencyObject d, object baseValue)
        {
            double? value = (double?)baseValue;
            return value.HasValue ? Math.Max(0.0, Math.Min(1.0, value.Value)) : 0.0;
        }

        private void DrawImage(DrawingContext context, BitmapSource image, Int32Rect sourceRect, Rect destRect)
        {
            try
            {
                CroppedBitmap croppedBitmap = new CroppedBitmap(image, sourceRect);
                context.DrawImage(croppedBitmap, destRect);
            } catch (Exception)
            {
                context.DrawImage(image, destRect);
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            BitmapSource bitmapSource = null;

            // If the Image is null and Background is set, create a solid color "image"
            if (Image == null && Background != null)
            {
                // Create a "fake" image filled with the background color
                bitmapSource = CreateSolidColorBitmap(Background);
                // set Image
            }
            else if (Image is BitmapSource source && source.PixelWidth > 0 && source.PixelHeight > 0)
            {
                // Use the actual image if present
                bitmapSource = source;

            }

            if (bitmapSource == null)
            {
                // Clear the control if both the image and background are null
                drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));
                return;
            }

            // Apply tint to the image before drawing
            BitmapSource tintedImage = ApplyTint(bitmapSource, TintColor, Darken ?? 0.0);

            // Perform nine-slice rendering using the tinted image
            DrawNineSliceImage(drawingContext, tintedImage);
        }

        private BitmapSource CreateSolidColorBitmap(Brush backgroundBrush)
        {
            if (backgroundBrush == null) return null;

            int width = (int)ActualWidth > 0 ? (int)ActualWidth : 100;
            int height = (int)ActualHeight > 0 ? (int)ActualHeight : 100;

            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawRectangle(backgroundBrush, null, new Rect(0, 0, width, height));
            }

            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(drawingVisual);

            return bitmap;
        }


        private BitmapSource ApplyTint(BitmapSource originalImage, Color tintColor, double darken)
        {
            // Get the pixel data of the original image
            var width = originalImage.PixelWidth;
            var height = originalImage.PixelHeight;
            var stride = width * ((originalImage.Format.BitsPerPixel + 7) / 8);
            var pixelData = new byte[height * stride];
            originalImage.CopyPixels(pixelData, stride, 0);

            // Convert the tint color to byte values
            byte tintR = ApplyDarken(tintColor.R, darken);
            byte tintG = ApplyDarken(tintColor.G, darken);
            byte tintB = ApplyDarken(tintColor.B, darken);

            // Apply the overlay blend mode pixel by pixel
            for (int i = 0; i < pixelData.Length; i += 4) // assuming 32-bit BGRA
            {
                // Get the original image pixel's color
                byte b = pixelData[i];     // Blue
                byte g = pixelData[i + 1]; // Green
                byte r = pixelData[i + 2]; // Red

                // Apply the overlay blend mode
                pixelData[i] = OverlayBlend(b, tintB);     // Blue
                pixelData[i + 1] = OverlayBlend(g, tintG); // Green
                pixelData[i + 2] = OverlayBlend(r, tintR); // Red
            }

            // Create a new BitmapSource from the modified pixel data
            var newImage = BitmapSource.Create(
                width,
                height,
                originalImage.DpiX,
                originalImage.DpiY,
                originalImage.Format,
                null,
                pixelData,
                stride
            );

            return newImage;
        }

        // Helper function to apply the darken effect
        private byte ApplyDarken(byte colorChannel, double darken)
        {
            return (byte)(colorChannel * (1 - darken));
        }

        // Helper function to apply the overlay blend mode to individual channels
        private byte OverlayBlend(byte baseChannel, byte tintChannel)
        {
            double baseNormalized = baseChannel / 255.0;
            double tintNormalized = tintChannel / 255.0;

            // Overlay blend formula
            double result;
            if (baseNormalized < 0.5)
            {
                result = 2 * baseNormalized * tintNormalized;
            }
            else
            {
                result = 1 - 2 * (1 - baseNormalized) * (1 - tintNormalized);
            }

            return (byte)(result * 255);
        }

        private void DrawNineSliceImage(DrawingContext context, BitmapSource image)
        {
            if (image == null) return;

            // if slice width is smaller than image width or height smaller than height, just draw the img
            if (Slice.Width >= image.PixelWidth || Slice.Height >= image.PixelHeight)
            {
                DrawImage(context, image, new Int32Rect(0, 0, image.PixelWidth, image.PixelHeight),
                    new Rect(0, 0, image.PixelWidth, image.PixelHeight));
                return;
            }

            // Same logic as in the base NineSlice class, but with the tinted image
            DrawImage(context, image, new Int32Rect(0, 0, (int)Math.Max(Slice.Width, 0), (int)Math.Max(Slice.Height, 0)),
                new Rect(0, 0, Math.Max(Slice.Width, 0), Math.Max(Slice.Height, 0)));

            DrawImage(context, image, new Int32Rect((int)Math.Max(Slice.Width, 0), 0,
                (int)Math.Max(image.Width - Slice.Width * 2, 0), (int)Math.Max(Slice.Height, 0)),
                new Rect(Math.Max(Slice.Width, 0), 0, Math.Max(ActualWidth - Slice.Width * 2, 0), Math.Max(Slice.Height, 0)));

            DrawImage(context, image, new Int32Rect((int)Math.Max(image.Width - Slice.Width, 0), 0,
                (int)Math.Max(Slice.Width, 0), (int)Math.Max(Slice.Height, 0)),
                new Rect(Math.Max(ActualWidth - Slice.Width, 0), 0, Math.Max(Slice.Width, 0), Math.Max(Slice.Height, 0)));

            DrawImage(context, image, new Int32Rect(0, (int)Math.Max(Slice.Height, 0),
                (int)Math.Max(Slice.Width, 0), (int)Math.Max(image.Height - Slice.Height * 2, 0)),
                new Rect(0, Math.Max(Slice.Height, 0), Math.Max(Slice.Width, 0), Math.Max(ActualHeight - Slice.Height * 2, 0)));

            DrawImage(context, image, new Int32Rect((int)Math.Max(Slice.Width, 0), (int)Math.Max(Slice.Height, 0),
                (int)Math.Max(image.Width - Slice.Width * 2, 0), (int)Math.Max(image.Height - Slice.Height * 2, 0)),
                new Rect(Math.Max(Slice.Width, 0), Math.Max(Slice.Height, 0), Math.Max(ActualWidth - Slice.Width * 2, 0), Math.Max(ActualHeight - Slice.Height * 2, 0)));

            DrawImage(context, image, new Int32Rect((int)Math.Max(image.Width - Slice.Width, 0), (int)Math.Max(Slice.Height, 0),
                (int)Math.Max(Slice.Width, 0), (int)Math.Max(image.Height - Slice.Height * 2, 0)),
                new Rect(Math.Max(ActualWidth - Slice.Width, 0), Math.Max(Slice.Height, 0), Math.Max(Slice.Width, 0), Math.Max(ActualHeight - Slice.Height * 2, 0)));

            DrawImage(context, image, new Int32Rect(0, (int)Math.Max(image.Height - Slice.Height, 0),
                (int)Math.Max(Slice.Width, 0), (int)Math.Max(Slice.Height, 0)),
                new Rect(0, Math.Max(ActualHeight - Slice.Height, 0), Math.Max(Slice.Width, 0), Math.Max(Slice.Height, 0)));

            DrawImage(context, image, new Int32Rect((int)Math.Max(Slice.Width, 0), (int)Math.Max(image.Height - Slice.Height, 0),
                (int)Math.Max(image.Width - Slice.Width * 2, 0), (int)Math.Max(Slice.Height, 0)),
                new Rect(Math.Max(Slice.Width, 0), Math.Max(ActualHeight - Slice.Height, 0), Math.Max(ActualWidth - Slice.Width * 2, 0), Math.Max(Slice.Height, 0)));

            DrawImage(context, image, new Int32Rect((int)Math.Max(image.Width - Slice.Width, 0), (int)Math.Max(image.Height - Slice.Height, 0),
                (int)Math.Max(Slice.Width, 0), (int)Math.Max(Slice.Height, 0)),
                new Rect(Math.Max(ActualWidth - Slice.Width, 0), Math.Max(ActualHeight - Slice.Height, 0), Math.Max(Slice.Width, 0), Math.Max(Slice.Height, 0)));
        }
    }
}