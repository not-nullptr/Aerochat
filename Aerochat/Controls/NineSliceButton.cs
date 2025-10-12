using Aerochat.Controls.NineSliceStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Aerochat.Controls
{
    enum ButtonState
    {
        Normal,
        Hover,
        Pressed,
        Disabled
    }

    public enum ToggleMode // OmegaAOL
    {
        NoToggle, 
        CheckToggleButton, // emulates checkbox - click to toggle and untoggle
        RadioToggleButton, // emulates radio button - can only be untoggled programatically (button.SetToggle(false)) once toggled
    }

    /// <summary>
    /// A variant of the nine-slice background control suited for buttons. This automatically handles the changing of the
    /// background based on mouse interactions.
    /// </summary>
    public class NineSliceButton : NineSlice
    {
        protected NineSliceImageSet? _hoverImageSet = null;
        protected NineSliceImageSet? _pressedImageSet = null;
        //protected NineSliceImageSet? _disabledImageSet = null;

        private ButtonState _state = ButtonState.Normal;
        private ButtonState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnStateChanged();
                }
            }
        }

        public ImageSource? Normal
        {
            get { return (ImageSource)GetValue(NormalProperty); }
            set { SetValue(NormalProperty, value); InvalidateVisual(); }
        }

        public static readonly DependencyProperty NormalProperty =
            DependencyProperty.Register("Normal", typeof(ImageSource), typeof(NineSliceButton), new PropertyMetadata(default(ImageSource), OnImageChanged));

        public ImageSource? Hover
        {
            get { return (ImageSource)GetValue(HoverProperty); }
            set { SetValue(HoverProperty, value); InvalidateVisual(); }
        }

        public static readonly DependencyProperty HoverProperty =
            DependencyProperty.Register("Hover", typeof(ImageSource), typeof(NineSliceButton), new PropertyMetadata(default(ImageSource), OnImageChanged));

        public ImageSource? Pressed
        {
            get { return (ImageSource)GetValue(PressedProperty); }
            set { SetValue(PressedProperty, value); InvalidateVisual(); }
        }

        public ToggleMode ToggleButton { get; set; } = ToggleMode.NoToggle;
        private bool IsToggled = false;
        public void SetToggle(bool toggle)
        {
            if (toggle)
            {
                State = ButtonState.Pressed;
            }
            else
            {
                State = ButtonState.Normal;
                InvalidateVisual();
            }

            IsToggled = toggle;
        }

        public static readonly DependencyProperty PressedProperty =
            DependencyProperty.Register("Pressed", typeof(ImageSource), typeof(NineSliceButton), new PropertyMetadata(default(ImageSource), OnImageChanged));

        protected override void ReloadImage()
        {
            if (Normal != null && (Normal is BitmapSource normalBitmapSource) && normalBitmapSource.PixelWidth != 0 && normalBitmapSource.PixelHeight != 0)
            {
                _imageSet = NineSliceCacheManager.Instance.FindOrCreateImageSet(normalBitmapSource, Slice);
            }

            if (Hover != null && (Hover is BitmapSource hoverBitmapSource) && hoverBitmapSource.PixelWidth != 0 && hoverBitmapSource.PixelHeight != 0)
            {
                _hoverImageSet = NineSliceCacheManager.Instance.FindOrCreateImageSet(hoverBitmapSource, Slice);
            }

            if (Pressed != null && (Pressed is BitmapSource pressedBitmapSource) && pressedBitmapSource.PixelWidth != 0 && pressedBitmapSource.PixelHeight != 0)
            {
                _pressedImageSet = NineSliceCacheManager.Instance.FindOrCreateImageSet(pressedBitmapSource, Slice);
            }
        }

        protected override CroppedBitmap? ResolvePartBitmap(NineSlicePart part)
        {
            if (State == ButtonState.Hover)
            {
                return ResolvePartBitmapFromImageSet(_hoverImageSet, part);
            }
            else if (State == ButtonState.Pressed)
            {
                return ResolvePartBitmapFromImageSet(_pressedImageSet, part);
            }

            return ResolvePartBitmapFromImageSet(_imageSet, part);
        }

        private void OnStateChanged()
        {
            if (!IsToggled)
            {
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            State = ButtonState.Hover;

        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            State = ButtonState.Normal;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            State = ButtonState.Pressed;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (IsToggled && ToggleButton == ToggleMode.CheckToggleButton) { IsToggled = false; State = ButtonState.Hover; }
            else
            {
                if (ToggleButton > 0) // for both CheckToggleButton and RadioToggleButton
                {
                    State = ButtonState.Pressed;
                    IsToggled = true;
                }
                else
                {
                    State = ButtonState.Hover;
                }
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            InvalidateVisual();
        }
    }
}
