using Aerochat.ViewModels;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Aerochat.Hoarder
{
    public static class Discord
    {   
        public static DiscordClient Client;

        public static bool Ready = false;

        public static List<BitmapImage> ProfileFrames =
        [
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/LargeFrameActive.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/LargeFrameActiveAnimation.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/LargeFrameDnd.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/LargeFrameDndAnimation.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/LargeFrameIdle.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/LargeFrameIdleAnimation.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/LargeFrameOffline.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/MediumFrameActive.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/MediumFrameDnd.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/MediumFrameDndAnimation.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/MediumFrameIdle.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/MediumFrameIdleAnimation.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/MediumFrameOffline.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/MediumFrameActiveAnimation.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/SmallFrameActive.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/SmallFrameActiveAnimation.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/SmallFrameDnd.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/SmallFrameDndAnimation.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/SmallFrameIdle.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/SmallFrameIdleAnimation.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/SmallFrameOffline.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/XLFrameActive.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/XLFrameActiveAnimation.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/XLFrameDnd.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/XLFrameDndAnimation.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/XLFrameIdle.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/XLFrameIdleAnimation.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/XLFrameOffline.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/XSFrameActive.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/XSFrameActiveM.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/XSFrameDnd.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/XSFrameDndM.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/XSFrameIdle.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/XSFrameIdleM.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/XSFrameOffline.png")),
            new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Frames/XSFrameOfflineM.png")),
        ];
    }
}
