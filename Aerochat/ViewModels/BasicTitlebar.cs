using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Aerochat.ViewModels
{
    public class BasicTitlebarViewModel : ViewModelBase
    {
        private string _title;
        private ImageSource _icon;
        private bool _activated;
        private Brush _color;
        private Brush _textColor;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
        public ImageSource Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }
        public bool Activated
        {
            get => _activated;
            set => SetProperty(ref _activated, value);
        }
        public Brush Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }
        public Brush TextColor
        {
            get => _textColor;
            set => SetProperty(ref _textColor, value);
        }
    }
}
