using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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

        public static AdViewModel FromAd(XElement ad)
        {
            return new()
            {
                // Image and Url are required, they are properties on the xelement, like <Ad Image="..." Url="..." />
                Image = $"/Ads/{ad.Attribute("Image")?.Value ?? throw new ArgumentException("Ad must have an Image")}",
                Url = ad.Attribute("Url")?.Value ?? throw new ArgumentException("Ad must have a Url")
            };
        }
    }
}
