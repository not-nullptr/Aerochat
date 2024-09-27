using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Aerochat.Controls.ScrollBar
{
    /// <summary>
    /// I love WPF.
    /// </summary>
    internal static class ScrollBarProperties
    {
        public static bool GetShowJumpToBottom(DependencyObject obj)
        {
            return (bool)obj.GetValue(ShowJumpToBottomProperty);
        }

        public static void SetShowJumpToBottom(DependencyObject obj, bool value)
        {
            obj.SetValue(ShowJumpToBottomProperty, value);
        }

        public static readonly DependencyProperty ShowJumpToBottomProperty =
            DependencyProperty.RegisterAttached(
                "ShowJumpToBottom",
                typeof(bool),
                typeof(ScrollBarProperties),
                new FrameworkPropertyMetadata(false)
            );

        public static bool GetShowJumpToTop(DependencyObject obj)
        {
            return (bool)obj.GetValue(ShowJumpToTopProperty);
        }

        public static void SetShowJumpToTop(DependencyObject obj, bool value)
        {
            obj.SetValue(ShowJumpToTopProperty, value);
        }

        public static readonly DependencyProperty ShowJumpToTopProperty =
            DependencyProperty.RegisterAttached(
                "ShowJumpToTop",
                typeof(bool),
                typeof(ScrollBarProperties),
                new FrameworkPropertyMetadata(false)
            );

        public static bool GetShowJumpToLeft(DependencyObject obj)
        {
            return (bool)obj.GetValue(ShowJumpToLeftProperty);
        }

        public static void SetShowJumpToLeft(DependencyObject obj, bool value)
        {
            obj.SetValue(ShowJumpToLeftProperty, value);
        }

        public static readonly DependencyProperty ShowJumpToLeftProperty =
            DependencyProperty.RegisterAttached(
                "ShowJumpToLeft",
                typeof(bool),
                typeof(ScrollBarProperties),
                new FrameworkPropertyMetadata(false)
            );

        public static bool GetShowJumpToRight(DependencyObject obj)
        {
            return (bool)obj.GetValue(ShowJumpToRightProperty);
        }

        public static void SetShowJumpToRight(DependencyObject obj, bool value)
        {
            obj.SetValue(ShowJumpToRightProperty, value);
        }

        public static readonly DependencyProperty ShowJumpToRightProperty =
            DependencyProperty.RegisterAttached(
                "ShowJumpToRight",
                typeof(bool),
                typeof(ScrollBarProperties),
                new FrameworkPropertyMetadata(false)
            );
    }
}
