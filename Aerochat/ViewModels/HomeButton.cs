using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.ViewModels
{
    public class HomeButtonViewModel : ViewModelBase
    {
        private string _image;

        public string Image
        {
            get => _image;
            set => SetProperty(ref _image, value);
        }

        public Action? Click { get; set; }
    }
}
