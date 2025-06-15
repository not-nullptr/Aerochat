using Aerochat.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Aerochat.Controls.NineSliceStuff;

/// <summary>
/// Manages a cache for shared nine-slice images. This is useful because a lot of our nine-slice
/// images appear several times throughout the application as they are often used for common
/// control backgrounds.
/// </summary>
internal class NineSliceCacheManager
{
    public static NineSliceCacheManager Instance { get; private set; } = new();

    private WeakDictionary<string, NineSliceImageSet> _entries = [];

    /// <summary>
    /// Attempts to find an already existing image set meeting the same parameters, or creates a new one
    /// if no cache entry exists.
    /// </summary>
    /// <returns>A nine-slice image set.</returns>
    public NineSliceImageSet FindOrCreateImageSet(BitmapSource bitmapSource, Size slice)
    {
        BitmapImage? bitmapImage = bitmapSource as BitmapImage;
        string? uri = bitmapImage?.UriSource?.ToString() ?? null;

        if (uri == null)
        {
            BitmapFrame? bitmapFrame = bitmapSource as BitmapFrame;
            uri = bitmapFrame?.Decoder.ToString() ?? null;
        }

        if (uri is not null)
        {
            // If we have a URI, then we are eligible for caching.
            uri += $"{slice.Width}x{slice.Height}";

            if (_entries.TryGetValue(uri, out NineSliceImageSet? cachedResult) && cachedResult is not null)
            {
                return cachedResult;
            }
        }

        NineSliceImageSet result = NineSliceImageSet.FromBitmap(bitmapSource, slice);

        if (uri is not null) // Eligible for caching:
        {
            _entries.TryAdd(uri, result);
        }

        return result;
    }
}
