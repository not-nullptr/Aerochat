using Aerochat.Hoarder;
using Aerochat.Theme;
using Aerochat.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Aerochat.Windows
{
    public partial class ChangeScene : Window
    {
        public ChangeSceneViewModel ViewModel { get; } = new();
        public ChangeScene()
        {
            InitializeComponent();
            DataContext = ViewModel;
            foreach (var scene in ThemeService.Instance.Scenes)
            {
                ViewModel.Scenes.Add(new ChangeSceneItemViewModel { Scene = scene });
            }
            ViewModel.Scenes[ThemeService.Instance.Scene.Id - 1].Selected = true;
        }

        private async void Border_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border is null) return;
            var item = border.DataContext as ChangeSceneItemViewModel;
            // parse item color into 0xRRGGBB format (it is currently a string, #RRGGBB)
            if (item is null) return;
            int R = Convert.ToInt32(item.Scene.Color.Substring(1, 2), 16);
            int G = Convert.ToInt32(item.Scene.Color.Substring(3, 2), 16);
            int B = Convert.ToInt32(item.Scene.Color.Substring(5, 2), 16);
            int color = (R << 16) | (G << 8) | B;
            foreach (var scene in ViewModel.Scenes)
            {
                scene.Selected = false;
            }
            item.Selected = true;
            SceneList.IsHitTestVisible = false;
            SceneList.Opacity = 0.5;
            OkButton.IsEnabled = false;
            CloseButton.IsEnabled = false;
            ApplyButton.IsEnabled = false;
            await Discord.Client.UpdateBannerColorAsync(color);
            ThemeService.Instance.Scene = item.Scene;
            foreach (var wnd in Application.Current.Windows)
            {
                if (wnd is Chat chat && !chat.ViewModel.IsDM && chat.ViewModel.Recipient is not null)
                {
                    chat.ViewModel.Recipient.Scene = item.Scene;
                }
            }
            SceneList.IsHitTestVisible = true;
            SceneList.Opacity = 1;
            OkButton.IsEnabled = true;
            CloseButton.IsEnabled = true;
            ApplyButton.IsEnabled = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
