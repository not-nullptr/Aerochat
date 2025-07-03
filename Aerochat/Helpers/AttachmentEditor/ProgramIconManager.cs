using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.Shell32;

namespace Aerochat.Helpers.AttachmentEditor
{
    public class ProgramIconManager
    {
        public BitmapSource LoadIcon(string filePath)
        {
            return LoadIconFromShellApi(filePath);
        }

        private BitmapSource LoadIconFromShellApi(string filePath)
        {
            SHFILEINFO fileInfo = new();

            try
            {
                IntPtr hImgSmall = SHGetFileInfo(
                    filePath, 0, ref fileInfo, Marshal.SizeOf(typeof(SHFILEINFO)),
                    SHGFI.SHGFI_ICON | SHGFI.SHGFI_USEFILEATTRIBUTES | SHGFI.SHGFI_LARGEICON
                );

                if (fileInfo.hIcon != 0)
                {
                    BitmapSource result = Imaging.CreateBitmapSourceFromHIcon(
                        (nint)fileInfo.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(32, 32)
                    );
                    result.Freeze();

                    return result;
                }

                return null;
            }
            finally
            {
                if (fileInfo.hIcon != 0)
                {
                    DestroyIcon(fileInfo.hIcon);
                }
            }
        }
    }
}
