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
        private int _initialColor;
        public ChangeSceneViewModel ViewModel { get; } = new();
        public ChangeScene()
        {
            InitializeComponent();
            DataContext = ViewModel;
            foreach (var scene in ThemeService.Instance.Scenes)
            {
                ViewModel.Scenes.Add(new ChangeSceneItemViewModel { Scene = scene });
            }

            // use Discord.Client.CurrentUser.BannerColor.R, G and B to get the current color
            _initialColor = Discord.Client.CurrentUser.BannerColor?.Value ?? 0;

            try
            {
                if (ViewModel.Scenes.Count > ThemeService.Instance.Scene.Id - 1)
                {
                    ViewModel.Scenes[ThemeService.Instance.Scene.Id - 1].Selected = true;
                }
            } catch (Exception) { }
        }

        private void Border_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border is null) return;
            var item = border.DataContext as ChangeSceneItemViewModel;
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
            ThemeService.Instance.Scene = item.Scene;
            foreach (var wnd in Application.Current.Windows)
            {
                if (wnd is Chat chat && (!chat.ViewModel.IsDM || chat.ViewModel.IsGroupChat) && chat.ViewModel.Recipient is not null)
                {
                    chat.ViewModel.Recipient.Scene = item.Scene;
                }
            }
            
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn is null) return;

            if (btn.Name == "CloseButton")
            {
                // if the color isn't the same as the initial color, revert it
                if (ThemeService.Instance.Scene.Color.ToLower() != $"#{Discord.Client.CurrentUser.BannerColor?.Value:X6}".ToLower())
                {
                    ThemeService.Instance.Scene = ThemeService.Instance.Scenes.FirstOrDefault(s => s.Color.ToLower() == $"#{Discord.Client.CurrentUser.BannerColor?.Value:X6}".ToLower()) ?? ThemeService.Instance.Scenes.FirstOrDefault(s => s.Default);
                    foreach (var wnd in Application.Current.Windows)
                    {
                        if (wnd is Chat chat && (!chat.ViewModel.IsDM || chat.ViewModel.IsGroupChat) && chat.ViewModel.Recipient is not null)
                        {
                            chat.ViewModel.Recipient.Scene = ThemeService.Instance.Scene;
                        }
                    }
                }
                Close();
                return;
            }

            var selectedItem = ViewModel.Scenes.FirstOrDefault(scene => scene.Selected);

            if (selectedItem is null) return;

            if (selectedItem.Scene.Color == $"#{Discord.Client.CurrentUser.BannerColor?.Value:X6}")
            {
                if (btn.Name == "OkButton") Close();
                return;
            }

            int R = Convert.ToInt32(selectedItem.Scene.Color.Substring(1, 2), 16);
            int G = Convert.ToInt32(selectedItem.Scene.Color.Substring(3, 2), 16);
            int B = Convert.ToInt32(selectedItem.Scene.Color.Substring(5, 2), 16);
            int color = (R << 16) | (G << 8) | B;

            SceneList.IsHitTestVisible = false;
            SceneList.Opacity = 0.5;
            OkButton.IsEnabled = false;
            CloseButton.IsEnabled = false;
            ApplyButton.IsEnabled = false;

            await Discord.Client.UpdateBannerColorAsync(color);
            ThemeService.Instance.Scene = selectedItem.Scene;

            foreach (var wnd in Application.Current.Windows)
            {
                if (wnd is Chat chat && (!chat.ViewModel.IsDM || chat.ViewModel.IsGroupChat) && chat.ViewModel.Recipient is not null)
                {
                    chat.ViewModel.Recipient.Scene = selectedItem.Scene;
                }
            }

            SceneList.IsHitTestVisible = true;
            SceneList.Opacity = 1;
            OkButton.IsEnabled = true;
            CloseButton.IsEnabled = true;
            ApplyButton.IsEnabled = true;

            if (btn.Name != "ApplyButton")
            {
                Close();
            } else
            {
                _initialColor = Discord.Client.CurrentUser.BannerColor?.Value ?? 0;
            }
        }
    }
}
