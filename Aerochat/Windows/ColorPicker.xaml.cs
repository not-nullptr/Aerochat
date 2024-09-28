using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Aerochat.Windows
{
    public partial class ColorPicker : Window
    {
        public SolidColorBrush SelectedColor { get; set; } = null!;
        private bool isClosing = false;
        public ColorPicker()
        {
            InitializeComponent();
            // bring to front, focus, etc
            Topmost = true;
            Focus();
            Activate();
            Deactivated += ColorPicker_Deactivated;
        }

        private void ColorPicker_Deactivated(object? sender, EventArgs e)
        {
            // if we're already closing, don't close again
            if (isClosing) return;
            isClosing = true;
            Close();
        }

        private void SelectColor(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            // find first ancestor of type Rectangle
            var rect = FindChild<Rectangle>(btn);
            if (rect is null) return;
            var color = (SolidColorBrush)rect.Fill;
            SelectedColor = color;
            isClosing = true;
            Close();
        }

        private T? FindChild<T>(FrameworkElement control)
        {
            // recursively look through children until we find it
            foreach (var child in LogicalTreeHelper.GetChildren(control))
            {
                if (child is T t)
                {
                    return t;
                } else
                {
                    return FindChild<T>((FrameworkElement)child);
                }
            }
            return default;
        }
    }
}
