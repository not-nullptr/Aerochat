using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.ViewModels
{
    public class AdViewModel : ViewModelBase
    {
        private string _image = "/Resources/Ads/N09.png";
        private string _url;

        public string Image
        {
            get => _image;
            set => SetProperty(ref _image, value);
        }

        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }
    }
}
