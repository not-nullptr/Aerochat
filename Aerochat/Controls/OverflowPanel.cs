using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Aerochat.Controls
{
    public class OverflowPanel : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            double totalWidth = 0;
            int i = 0;
            foreach (UIElement child in InternalChildren)
            {
                child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
                totalWidth += child.DesiredSize.Width;
                // make the child visible again
                child.Visibility = Visibility.Visible;
                if (totalWidth > availableSize.Width - 32)
                {
                    totalWidth = availableSize.Width;
                    for (int j = i; j < InternalChildren.Count; j++)
                    {
                        InternalChildren[j].Visibility = Visibility.Hidden;
                    }
                    return new Size(totalWidth, availableSize.Height);
                }
                i++;
            }
            return new Size(totalWidth, availableSize.Height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double offsetX = 0;
            foreach (UIElement child in InternalChildren)
            {
                double childRightEdge = offsetX + child.DesiredSize.Width;
                child.Arrange(new Rect(new Point(offsetX, 0), child.DesiredSize));
                offsetX = childRightEdge;
            }
            return finalSize;
        }
    }
}
