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
                    filePath, 0, ref fileInfo, (uint)Marshal.SizeOf(typeof(SHFILEINFO)),
                    (uint)(SHGFI.Icon | SHGFI.UseFileAttributes | SHGFI.LargeIcon)
                );

                if (fileInfo.hIcon != 0)
                {
                    BitmapSource result = Imaging.CreateBitmapSourceFromHIcon(
                        fileInfo.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(32, 32)
                    );

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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [Flags]
        enum SHGFI : int
        {
            /// <summary>get icon</summary>
            Icon = 0x000000100,
            /// <summary>get display name</summary>
            DisplayName = 0x000000200,
            /// <summary>get type name</summary>
            TypeName = 0x000000400,
            /// <summary>get attributes</summary>
            Attributes = 0x000000800,
            /// <summary>get icon location</summary>
            IconLocation = 0x000001000,
            /// <summary>return exe type</summary>
            ExeType = 0x000002000,
            /// <summary>get system icon index</summary>
            SysIconIndex = 0x000004000,
            /// <summary>put a link overlay on icon</summary>
            LinkOverlay = 0x000008000,
            /// <summary>show icon in selected state</summary>
            Selected = 0x000010000,
            /// <summary>get only specified attributes</summary>
            Attr_Specified = 0x000020000,
            /// <summary>get large icon</summary>
            LargeIcon = 0x000000000,
            /// <summary>get small icon</summary>
            SmallIcon = 0x000000001,
            /// <summary>get open icon</summary>
            OpenIcon = 0x000000002,
            /// <summary>get shell size icon</summary>
            ShellIconSize = 0x000000004,
            /// <summary>pszPath is a pidl</summary>
            PIDL = 0x000000008,
            /// <summary>use passed dwFileAttribute</summary>
            UseFileAttributes = 0x000000010,
            /// <summary>apply the appropriate overlays</summary>
            AddOverlays = 0x000000020,
            /// <summary>Get the index of the overlay in the upper 8 bits of the iIcon</summary>
            OverlayIndex = 0x000000040,
        }

        [DllImport("user32.dll", SetLastError = true)]
        extern static bool DestroyIcon(IntPtr hIcon);
    }
}
