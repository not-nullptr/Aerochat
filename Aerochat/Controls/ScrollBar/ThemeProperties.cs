using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Aerochat.Controls.ScrollBar
{
    /// <summary>
    /// Custom properties used for refactored code.
    /// </summary>
    /// <remarks>
    /// These are used as custom properties on XAML objects in order to communicate state
    /// between templates. Bit hacky, but better than repeating yourself a hundred times
    /// over.
    /// </remarks>
    /// <see href="https://thomaslevesque.com/2011/10/01/wpf-creating-parameterized-styles-with-attached-properties/"/>
    internal static class ThemeProperties
    {
        public static Uri GetInactiveGlyph(DependencyObject obj)
        {
            return (Uri)obj.GetValue(InactiveGlyphProperty);
        }

        public static void SetInactiveGlyph(DependencyObject obj, Uri value)
        {
            obj.SetValue(InactiveGlyphProperty, value);
        }

        public static readonly DependencyProperty InactiveGlyphProperty =
            DependencyProperty.RegisterAttached(
                "InactiveGlyph",
                typeof(Uri),
                typeof(ThemeProperties),
                new FrameworkPropertyMetadata(null)
            );

        public static Uri GetHoverGlyph(DependencyObject obj)
        {
            return (Uri)obj.GetValue(HoverGlyphProperty);
        }

        public static void SetHoverGlyph(DependencyObject obj, Uri value)
        {
            obj.SetValue(HoverGlyphProperty, value);
        }

        public static readonly DependencyProperty HoverGlyphProperty =
            DependencyProperty.RegisterAttached(
                "HoverGlyph",
                typeof(Uri),
                typeof(ThemeProperties),
                new FrameworkPropertyMetadata(null)
            );

        public static Uri GetActiveGlyph(DependencyObject obj)
        {
            return (Uri)obj.GetValue(ActiveGlyphProperty);
        }

        public static void SetActiveGlyph(DependencyObject obj, Uri value)
        {
            obj.SetValue(ActiveGlyphProperty, value);
        }

        public static readonly DependencyProperty ActiveGlyphProperty =
            DependencyProperty.RegisterAttached(
                "ActiveGlyph",
                typeof(Uri),
                typeof(ThemeProperties),
                new FrameworkPropertyMetadata(null)
            );

        public static object GetBackgroundStyle(DependencyObject obj)
        {
            return (object)obj.GetValue(BackgroundStyleProperty);
        }

        public static void SetBackgroundStyle(DependencyObject obj, object value)
        {
            obj.SetValue(BackgroundStyleProperty, value);
        }

        public static readonly DependencyProperty BackgroundStyleProperty =
            DependencyProperty.RegisterAttached(
                "BackgroundStyle",
                typeof(object),
                typeof(ThemeProperties),
                new FrameworkPropertyMetadata(null)
            );
    }
}
