using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Aerochat.Controls.ButtonTheme
{
    /// <summary>
    /// Provides the button background image.
    /// </summary>
    /// <remarks>
    /// Hacky implementation to get button sizing like Aero.NormalColor.
    /// </remarks>
    public class ButtonBackgroundImage : NineSlice
    {
        // EVIL HACK TO GET THE CONTENT ELEMENT
        // Basically, we just extend the NineSlice control and copy the sizing overrides from
        // Aero.NormalColor, but Microsoft's implementation used a Decorator. Since I wanted
        // to inherit the style from NineSlice and C# doesn't support multiple inheritance, I
        // opted for this insane hack instead:
        public UIElement ButtonContent
        {
            get
            {
                // Get the parent grid:
                Grid grid = (Grid)Parent;

                // The ContentPresenter must be the second item in the grid; the
                // first being ourself.
                return grid.Children[1];
            }
            set { }
        }

        /// <summary>
        /// Updates DesiredSize of the ButtonChrome.  Called by parent UIElement.  This is the first pass of layout.
        /// </summary>
        /// <remarks>
        /// ButtonChrome basically inflates the desired size of its one child by 2 on all four sides
        /// </remarks>
        /// <param name="availableSize">Available size is an "upper limit" that the return value should not exceed.</param>
        /// <returns>The ButtonChrome's desired size.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            Size desired;
            UIElement child = ButtonContent;

            System.Diagnostics.Debug.WriteLine("Child is " + (child == null ? "not there" : "there"));

            if (child != null)
            {
                Size childConstraint = new Size();
                bool isWidthTooSmall = (availableSize.Width < 4.0);
                bool isHeightTooSmall = (availableSize.Height < 4.0);

                if (!isWidthTooSmall)
                {
                    childConstraint.Width = availableSize.Width - 4.0;
                }
                if (!isHeightTooSmall)
                {
                    childConstraint.Height = availableSize.Height - 4.0;
                }

                child.Measure(childConstraint);

                desired = child.DesiredSize;

                System.Diagnostics.Debug.WriteLine("Height is " + desired.Height);

                if (!isWidthTooSmall)
                {
                    desired.Width += 4.0;
                }
                if (!isHeightTooSmall)
                {
                    desired.Height += 4.0;
                }
            }
            else
            {
                desired = new Size(Math.Min(4.0, availableSize.Width), Math.Min(4.0, availableSize.Height));
            }

            return desired;
        }

        /// <summary>
        /// ButtonChrome computes the position of its single child inside child's Margin and calls Arrange
        /// on the child.
        /// </summary>
        /// <remarks>
        /// ButtonChrome basically inflates the desired size of its one child by 2 on all four sides
        /// </remarks>
        /// <param name="finalSize">Size the ContentPresenter will assume.</param>
        protected override Size ArrangeOverride(Size finalSize)
        {
            Rect childArrangeRect = new Rect();

            childArrangeRect.Width = Math.Max(0d, finalSize.Width - 4.0);
            childArrangeRect.Height = Math.Max(0d, finalSize.Height - 4.0);
            childArrangeRect.X = (finalSize.Width - childArrangeRect.Width) * 0.5;
            childArrangeRect.Y = (finalSize.Height - childArrangeRect.Height) * 0.5;

            UIElement child = ButtonContent;
            if (child != null)
            {
                child.Arrange(childArrangeRect);
            }

            return finalSize;
        }
    }
}
