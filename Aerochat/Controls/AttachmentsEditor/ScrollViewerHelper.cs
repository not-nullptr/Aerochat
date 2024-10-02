using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;

namespace Aerochat.Controls.AttachmentsEditor
{
    /// <summary>
    /// Helper for horizontal scrolling with the mouse.
    /// </summary>
    /// <see href="https://stackoverflow.com/a/42047117" />
    public static class ScrollViewerHelper
    {
        public static readonly DependencyProperty ScrollWheelScrollsHorizontallyProperty =
            DependencyProperty.RegisterAttached(
                "ScrollWheelScrollsHorizontally",
                typeof(bool),
                typeof(ScrollViewerHelper),
                new PropertyMetadata(false, UseHorizontalScrollingChangedCallback)
            );

        public static void SetScrollWheelScrollsHorizontally(UIElement element, bool value)
            => element.SetValue(ScrollWheelScrollsHorizontallyProperty, value);
        public static bool GetScrollWheelScrollsHorizontally(UIElement element)
            => (bool)element.GetValue(ScrollWheelScrollsHorizontallyProperty);

        private static void UseHorizontalScrollingChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as ScrollViewer;

            if (element == null)
                throw new Exception("Attached property must be used with ScrollViewer.");

            if ((bool)e.NewValue)
            {
                element.PreviewMouseWheel += OnPreviewMouseWheel;
            }
            else
            {
                element.PreviewMouseWheel -= OnPreviewMouseWheel;
            }
        }

        private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs args)
        {
            var scrollViewer = (ScrollViewer)sender;

            if (scrollViewer == null)
                return;

            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + -args.Delta);

            args.Handled = true;
        }
    }
}
