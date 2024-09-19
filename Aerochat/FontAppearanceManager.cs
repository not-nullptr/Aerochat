using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using static Vanara.PInvoke.User32;

namespace Aerochat
{
    /// <summary>
    /// Determines if an application should use ClearType or aliased fonts.
    /// </summary>
    internal class FontAppearanceManager
    {
        private static FontAppearanceManager _instance;
        private TextRenderingMode _textRenderingMode = TextRenderingMode.Auto;

        public static FontAppearanceManager Instance
        {
            get => _instance;
            private set => _instance = value;
        }

        public TextRenderingMode TextRenderingMode
        {
            get => _textRenderingMode;
            private set { }
        }

        static FontAppearanceManager()
        {
            _instance = new FontAppearanceManager();
        }

        public FontAppearanceManager()
        {
            Refresh();
        }

        /// <summary>
        /// Refresh the font smoothing settings.
        /// </summary>
        /// <remarks>
        /// The font rendering settings can change during a window's lifetime. This should be able to be
        /// detected via looking at WM_SETTINGCHANGE in the window procedure.
        /// 
        /// Aerochat does have a custom window procedure right now, but it's in a separate module. Some
        /// tiny work will need to be done to generalise it for detecting the settings change. For now,
        /// detection only works at application startup and font setting changes made in the middle of
        /// the application running will be ignored.
        /// </remarks>
        public void Refresh()
        {
            bool bFontSmoothingEnabled = false;
            bool spiResult = SystemParametersInfo(SPI_GETFONTSMOOTHING, 0, ref bFontSmoothingEnabled, false);

            if (!spiResult)
            {
                // If we failed the SystemParametersInfo call for whatever reason, then we'll just let
                // WPF handle it. This shouldn't happen, but it may happen due to marshalling failures
                // (maybe a concern on Linux via Wine?).
                _textRenderingMode = TextRenderingMode.Auto;
                return;
            }

            // Check to see if the user has font smoothing enabled at all:

            if (!bFontSmoothingEnabled)
            {
                // If this is the case, then the user has completely disabled font smoothing. No fonts
                // should be smoothed under any circumstances.
                _textRenderingMode = TextRenderingMode.Aliased;
                return;
            }

            // Otherwise, check the user's font smoothing type:

            int uiType = FE_FONTSMOOTHINGSTANDARD;
            spiResult = SystemParametersInfo(SPI_GETFONTSMOOTHINGTYPE, 0, ref uiType, false);

            if (!spiResult)
            {
                // If we failed the SystemParametersInfo call for whatever reason, then we'll just let
                // WPF handle it. This shouldn't happen, but it can happen due to marshalling failures
                // (maybe a concern on Linux via Wine?).
                _textRenderingMode = TextRenderingMode.Auto;
                return;
            }

            if (uiType == FE_FONTSMOOTHINGCLEARTYPE)
            {
                // The user has ClearType enabled.
                _textRenderingMode = TextRenderingMode.ClearType;
            }
            else // assume FE_FONTSMOOTHINGSTANDARD
            {
                /*
                 * This will probably fall back to aliased, but I didn't just want to assume that in
                 * case of small differences.
                 * 
                 * Note that ClearType being disabled is not the same as font smoothing being disabled
                 * system wide. In the case of ClearType being disabled, pixelated fonts are preferred,
                 * but smooth fonts will be used in large scales and when they are the only option.
                 * 
                 * This appears to have the correct behaviour in this case: large fonts are still smoothed
                 * and small fonts are pixelated when pixel glyphs are available.
                 */
                _textRenderingMode = TextRenderingMode.Auto;
            }
        }

        private const int SPI_GETFONTSMOOTHING = 0x004A;
        private const int SPI_GETFONTSMOOTHINGTYPE = 0x200A;
        private const int FE_FONTSMOOTHINGSTANDARD = 1;
        private const int FE_FONTSMOOTHINGCLEARTYPE = 2;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SystemParametersInfo(int uiAction, uint uiParam, ref bool pvParam, bool fWinIni);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SystemParametersInfo(int uiAction, uint uiParam, ref int pvParam, bool fWinIni);
    }
}
