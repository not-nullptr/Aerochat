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
    /// A column definition which can be hidden based on a condition.
    /// </summary>
    /// <see href="https://stackoverflow.com/a/30215839"/>
    public class HidableColumnDefinition : ColumnDefinition
    {
        private GridLength _width;

        public bool IsHidden
        {
            get { return (bool)GetValue(IsHiddenProperty); }
            set { SetValue(IsHiddenProperty, value); }
        }

        public static readonly DependencyProperty IsHiddenProperty =
            DependencyProperty.Register(
                "IsHidden", 
                typeof(bool), 
                typeof(HidableColumnDefinition), 
                new PropertyMetadata(false, OnIsHiddenChanged)
            );

        public static void OnIsHiddenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var o = d as HidableColumnDefinition;
            o.Toggle((bool)e.NewValue);
        }

        public void Toggle(bool isHidden)
        {
            if (isHidden)
            {
                _width = Width;
                Width = new GridLength(0, GridUnitType.Star);
            }
            else
                Width = _width;
        }
    }
}
