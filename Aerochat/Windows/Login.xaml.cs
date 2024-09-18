using Aerochat.Hoarder;
using Aerochat.Theme;
using Aerochat.ViewModels;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
    public partial class Login : Window
    {
        public LoginWindowViewModel ViewModel { get; set; } = new LoginWindowViewModel();
        public Login(bool alreadyErrored = false)
        {
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.Scene = ThemeService.Instance.Scenes.FirstOrDefault(x => x.Default);
            if (alreadyErrored)
            {
                Show();
                ShowErrorDialog();
            }
        }

        private void ShowErrorDialog()
        {
            var dialog = new Dialog("We can't sign you in to Windows Live Messenger", "The Windows Live Messenger token you entered is incorrect.", SystemIcons.Information);
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // open the context menu
            Dropdown.PlacementTarget = (Button)sender;
            Dropdown.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            Dropdown.IsOpen = true;
        }

        private void Available_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LoginStatus = "Available";
        }

        private void Busy_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LoginStatus = "Busy";
        }

        private void Away_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LoginStatus = "Away";
        }

        private void AppearsOffline_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LoginStatus = "Appear offline";
        }

        private void Password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (Password.Password.Length == 0)
            {
                PasswordPlaceholder.Visibility = Visibility.Visible;
            }
            else
            {
                PasswordPlaceholder.Visibility = Visibility.Hidden;
            }
        }

        private TaskCompletionSource<string> mfaCompletionSource;

        private async void SignIn_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.NotLoggingIn = false;
            var app = (App)Application.Current;
            if (mfaCompletionSource != null && MFATextBoxParent.Visibility == Visibility.Visible)
            {
                mfaCompletionSource.SetResult(MFATextBox.Text);
                return;
            }
            bool rememberMe = RememberMe.IsChecked == true;
            UserStatus status = ViewModel.LoginStatus switch
            {
                "Available" => UserStatus.Online,
                "Busy" => UserStatus.DoNotDisturb,
                "Away" => UserStatus.Idle,
                "Appear offline" => UserStatus.Invisible,
                _ => UserStatus.Online
            };
            bool success = await app.BeginLogin(Password.Password, rememberMe, status);
            if (!success)
            {
                ViewModel.NotLoggingIn = true;
                ShowErrorDialog();
            }
        }

        private async void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            // are we below windows 10 1607?
            if (Environment.OSVersion.Version < new Version(10, 0, 1803, 0))
            {
                var dialog = new Dialog("Unsupported Windows version", "This feature is only available on Windows 10 version 1607 or newer.", SystemIcons.Information);
                dialog.Owner = this;
                dialog.ShowDialog();
                return;
            }
            DiscordLoginWV2 login = new DiscordLoginWV2();
            login.Owner = this;
            login.ShowDialog();
            var app = (App)Application.Current;
            // login using login.Token
            if (string.IsNullOrEmpty(login.Token)) return;
            ViewModel.NotLoggingIn = false;
            bool rememberMe = RememberMe.IsChecked == true;
            UserStatus status = ViewModel.LoginStatus switch
            {
                "Available" => UserStatus.Online,
                "Busy" => UserStatus.DoNotDisturb,
                "Away" => UserStatus.Idle,
                "Appear offline" => UserStatus.Invisible,
                _ => UserStatus.Online
            };
            bool success = await app.BeginLogin(login.Token, rememberMe, status);
            if (!success)
            {
                ViewModel.NotLoggingIn = true;
                var dialog = new Dialog("We can't sign you in to Discord", "An unknown error occured attempting to use this .NET Passport.", SystemIcons.Information);
                dialog.Owner = this;
                dialog.ShowDialog();
            }
        }
    }
}
