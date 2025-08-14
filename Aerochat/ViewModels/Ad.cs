using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Aerochat.ViewModels
{
    public enum AdImageType
    {
        /// <summary>
        /// A regular (i.e. PNG) image with a single frame.
        /// </summary>
        StaticImage,

        /// <summary>
        /// A regular GIF image with one or more frames.
        /// </summary>
        Gif,

        /// <summary>
        /// An animated ad image which uses a spritesheet.
        /// </summary>
        SpritesheetAnimation,
    }

    public class AdViewModel : ViewModelBase
    {
        private string _image="/Aerochat;component/Resources/Ads/Aerochat.png";
        private string _url = "";
        private AdImageType _imageType = AdImageType.StaticImage;
        private int _animationFrames = 0;
        private int _animationFramerate = 0;

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

        public AdImageType ImageType
        {
            get => _imageType;
            private set => SetProperty(ref _imageType, value);
        }

        public int AnimationFrames
        {
            get => _animationFrames;
            private set => SetProperty(ref _animationFrames, value);
        }

        public int AnimationFramerate
        {
            get => _animationFramerate;
            private set => SetProperty(ref _animationFramerate, value);
        }

        public static AdViewModel FromAd(XElement ad)
        {
            AdImageType imageType = AdImageType.StaticImage;
            int animationFrames = 1; // Default number of frames.
            int animationFramerate = 60; // Default framerate.

            if (ad.Attribute("Frames") is not null)
            {
                imageType = AdImageType.SpritesheetAnimation;
                animationFrames = int.Parse(ad.Attribute("Frames")!.Value);

                if (ad.Attribute("FrameDelay") is not null)
                {
                    animationFramerate = int.Parse(ad.Attribute("FrameDelay")!.Value);
                }
            }
            else if ((bool)(ad.Attribute("Image")?.Value?.EndsWith(".gif")))
            {
                imageType = AdImageType.Gif;
            }

            return new()
            {
                // Image and Url are required, they are properties on the xelement, like <Ad Image="..." Url="..." />
                Image = $"/Ads/{ad.Attribute("Image")?.Value ?? throw new ArgumentException("Ad must have an Image")}",
                Url = ad.Attribute("Url")?.Value ?? throw new ArgumentException("Ad must have a Url"),
                ImageType = imageType,
                AnimationFrames = animationFrames,
                AnimationFramerate = animationFramerate,
            };
        }
    }
}
