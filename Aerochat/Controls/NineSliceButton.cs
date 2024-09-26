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

    public class NineSliceButton : NineSlice
    {
        private ButtonState __state = ButtonState.Normal;
        private ButtonState _state { get => __state; set 
            { 
                __state = value;
                switch (value)
                {
                    case ButtonState.Normal:
                        _image = Normal;
                        break;
                    case ButtonState.Hover:
                        _image = Hover;
                        break;
                    case ButtonState.Pressed:
                        _image = Pressed;
                        break;
                }
                InvalidateVisual();
            } 
        }
        private ImageSource? _image
        {
            get => Image;
            set => Image = value;
        }

        public ImageSource? Normal { get; set; }
        public ImageSource? Hover { get; set; }
        public ImageSource? Pressed { get; set; }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            _state = ButtonState.Hover;
            InvalidateVisual();
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            _state = ButtonState.Normal;
            InvalidateVisual();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            _state = ButtonState.Pressed;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            _state = ButtonState.Hover;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            InvalidateVisual();
        }
    }
}
