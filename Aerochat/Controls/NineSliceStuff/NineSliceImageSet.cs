using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml.XPath;

namespace Aerochat.Controls.NineSliceStuff;

/// <summary>
/// Stores a nine-slice image set.
/// </summary>
public class NineSliceImageSet
{
    public CroppedBitmap TopLeft { get; set; } = null!;
    public CroppedBitmap TopCenter { get; set; } = null!;
    public CroppedBitmap TopRight { get; set; } = null!;
    public CroppedBitmap CenterLeft { get; set; } = null!;
    public CroppedBitmap CenterCenter { get; set; } = null!;
    public CroppedBitmap CenterRight { get; set; } = null!;
    public CroppedBitmap BottomLeft { get; set; } = null!;
    public CroppedBitmap BottomCenter { get; set; } = null!;
    public CroppedBitmap BottomRight { get; set; } = null!;

    protected NineSliceImageSet()
    {
    }

    public static NineSliceImageSet FromBitmap(BitmapSource bitmap, Size slice)
    {
        NineSliceImageSet result = new();

        /*
         * Cropped bitmaps must have a minimum width and height of 1, so that's the minimum value
         * we select for no matter what.
         */

        int sw = (int)Math.Max(slice.Width, 1); // Slice width
        int sh = (int)Math.Max(slice.Height, 1); // Slice height
        int iw = Math.Max(bitmap.PixelWidth, 0); // Image width
        int ih = Math.Max(bitmap.PixelHeight, 0); // Image height

        result.TopLeft = new(bitmap, new(
            0, 
            0, 
            sw, 
            sh));
        result.TopCenter = new(bitmap, new(
            sw, 
            0, 
            Math.Max(iw - sw * 2, 1), 
            sh));
        result.TopRight = new(bitmap, new(
            Math.Max(iw - sw, 0), 
            0, 
            sw, 
            sh));
        result.CenterLeft = new(bitmap, new(
            0, 
            sh, 
            sw, 
            Math.Max(ih - sh * 2, 1)));
        result.CenterCenter = new(bitmap, new(
            sw, 
            sh, 
            Math.Max(iw - sw * 2, 1),
            Math.Max(ih - sh * 2, 1)));
        result.CenterRight = new(bitmap, new(
            Math.Max(iw - sw, 0), 
            sh, 
            sw, 
            Math.Max(ih - sh * 2, 1)));
        result.BottomLeft = new(bitmap, new(
            0, 
            Math.Max(ih - sh, 0), 
            sw, 
            sh));
        result.BottomCenter = new(bitmap, new(
            sw,
            Math.Max(ih - sh, 0), 
            Math.Max(iw - sw * 2, 1),
            sh));
        result.BottomRight = new(bitmap, new(
            Math.Max(iw - sw, 0),
            Math.Max(ih - sh, 0), 
            sw,
            sh));

        // The nine-slice set won't be modified after this point, so we'll freeze the image for improved performance
        // and memory usage.
        result.TopLeft.Freeze();
        result.TopCenter.Freeze();
        result.TopRight.Freeze();
        result.CenterLeft.Freeze();
        result.CenterCenter.Freeze();
        result.CenterRight.Freeze();
        result.BottomLeft.Freeze();
        result.BottomCenter.Freeze();
        result.BottomRight.Freeze();

        return result;
    }
}
