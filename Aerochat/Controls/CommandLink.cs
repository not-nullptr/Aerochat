using Aerochat.HwndHosts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Vanara.PInvoke.User32;

namespace Aerochat.Controls
{
    public class CommandLink : UserControl
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(CommandLink), new PropertyMetadata("", OnTitleChanged));
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(CommandLink), new PropertyMetadata("", OnDescriptionChanged));
        public static readonly DependencyProperty ControlHostProperty = DependencyProperty.Register(nameof(Control), typeof(ControlHost), typeof(CommandLink), new PropertyMetadata(null));

        public event EventHandler? Click;

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }
        public ControlHost Control
        {
            get => (ControlHost)GetValue(ControlHostProperty);
            private set => SetValue(ControlHostProperty, value);
        }

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CommandLink commandLink)
            {
                commandLink.OnTitleChanged();
            }
        }

        private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CommandLink commandLink)
            {
                commandLink.OnDescriptionChanged();
            }
        }

        private void OnTitleChanged()
        {
            if (Control?.ControlHwnd is null) return;
            SendMessage(Control.ControlHwnd, WindowMessage.WM_SETTEXT, 0, Title);
        }

        private void OnDescriptionChanged()
        {
            if (Control?.ControlHwnd is null) return;
            SendMessage(Control.ControlHwnd, ButtonMessage.BCM_SETNOTE, 0, Description);
        }

        public CommandLink()
        {
            Control = new(ActualWidth, ActualHeight);
            Control.Click += ControlHost_Click;
            Content = Control;
            Control.WindowCreated += ControlHost_WindowCreated;

        }

        private void ControlHost_WindowCreated(object? sender, EventArgs e)
        {
            SendMessage(Control.ControlHwnd, WindowMessage.WM_SETTEXT, 0, Title);
            SendMessage(Control.ControlHwnd, ButtonMessage.BCM_SETNOTE, 0, Description);
        }

        private void ControlHost_Click(object? sender, EventArgs e)
        {
            Click?.Invoke(this, EventArgs.Empty);
        }
    }
}
