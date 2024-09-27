using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace Aerochat.Controls.ScrollBar
{
    /// <summary>
    /// A row definition which can be hidden based on a condition.
    /// </summary>
    /// <see href="https://stackoverflow.com/a/30215839"/>
    public class HidableRowDefinition : RowDefinition
    {
        private GridLength _height;

        public bool IsHidden
        {
            get { return (bool)GetValue(IsHiddenProperty); }
            set { SetValue(IsHiddenProperty, value); }
        }

        public static readonly DependencyProperty IsHiddenProperty =
            DependencyProperty.Register(
                "IsHidden", 
                typeof(bool), 
                typeof(HidableRowDefinition), 
                new PropertyMetadata(false, OnIsHiddenChanged)
            );

        public static void OnIsHiddenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var o = d as HidableRowDefinition;
            o.Toggle((bool)e.NewValue);
        }

        public void Toggle(bool isHidden)
        {
            if (isHidden)
            {
                _height = Height;
                Height = new GridLength(0, GridUnitType.Star);
            }
            else
                Height = _height;
        }
    }
}
