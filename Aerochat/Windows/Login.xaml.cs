using Aerochat.Enums;
using Aerochat.Hoarder;
using Aerochat.Settings;
using Aerochat.Theme;
using Aerochat.ViewModels;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Web.WebView2.Core;
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
        public const string HELP_LOGON_URI = "https://github.com/not-nullptr/Aerochat/wiki/Get-help-logging-in";
        public const string HELP_GET_TOKEN_URI = HELP_LOGON_URI + "#token-logon";

        public LoginWindowViewModel ViewModel { get; set; } = new LoginWindowViewModel();
        public Login(bool alreadyErrored = false, AerochatLoginStatus previousError = AerochatLoginStatus.Success)
        {
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.Scene = ThemeService.Instance.Scenes.FirstOrDefault(x => x.Default);

            DataObject.AddPastingHandler(Password, OnPasteIntoTokenInputBox);

            if (!SettingsManager.Instance.HasUserLoggedInBefore)
            {
                Show();
                var dialog = new Dialog("Warning", "Using a custom Discord client is against Discord's rules.\n\nBy using Aerochat, " +
                    "you risk the restriction or termination of your Discord account. If you do not wish to take this risk, please " +
                    "do NOT use Aerochat on any account that you care about.\n\nYOU WILL NOT BE WARNED AGAIN.", SystemIcons.Warning);
                dialog.Owner = this;
                dialog.ShowDialog();
            }

            if (alreadyErrored)
            {
                Show();
                ShowErrorDialog(previousError);
            }
        }

        private void ShowErrorDialog(AerochatLoginStatus loginStatus)
        {
            List<Inline> message = loginStatus switch
            {
                AerochatLoginStatus.Success => new() {
                    new Run("Invalid data passed to constructor. This is an error meant to be seen by Aerochat developers.") },

                AerochatLoginStatus.Unauthorized => new() {
                    new Run("The Discord token you entered is incorrect. "),
                    new Hyperlink(new Run("For help retrieving your Discord token, click here."))
                    {
                        NavigateUri = new Uri(HELP_GET_TOKEN_URI),
                    } },

                AerochatLoginStatus.BadRequest => new() {
                    new Run("The request Aerochat made is malformed and Discord did not accept it. This is a severe Aerochat bug.") },

                AerochatLoginStatus.ServerError => new() {
                    new Run("There was an error connecting to the server. Please check your internet connection and firewall " +
                            "settings to ensure that Aerochat has access to the internet.") },

                _ => new() { new Run("An unknown error occurred. Please try again later.") }
            };

            var dialog = new Dialog("We can't sign you in to Discord", message, SystemIcons.Information);
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
                ViewModel.EditBoxHasContent = false;
            }
            else
            {
                PasswordPlaceholder.Visibility = Visibility.Hidden;
                ViewModel.EditBoxHasContent = true;
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

            AerochatLoginStatus loginStatus = await app.BeginLogin(TransformTokenForConsumption(Password.Password), rememberMe, status);
            if (loginStatus != AerochatLoginStatus.Success)
            {
                ViewModel.NotLoggingIn = true;
                ShowErrorDialog(loginStatus);
            }
        }

        /// <summary>
        /// Transforms a Discord token for consumption by removing surrounding information, such as
        /// whitespace and keywords from browser console such as "Authorization:".
        /// </summary>
        /// <remarks>
        /// This function was written because of many people coming into support channels to ask why
        /// their tokens weren't working, even when they copied it from their browser's developer tools.
        /// Turns out that they didn't understand how whitespace or other surrounding words needed to be
        /// removed, so we should just handle it on our end for their sake.
        /// </remarks>
        private string TransformTokenForConsumption(string originalToken)
        {
            string processedToken = originalToken.Replace("Authorization:", "", true, null)
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim();
            return processedToken;
        }

        private async void OnClickLoginWithPassword(object sender, RoutedEventArgs e)
        {
            string ver = "";
            try
            {
                ver = CoreWebView2Environment.GetAvailableBrowserVersionString();
            }
            catch (Exception) { }
            if (string.IsNullOrEmpty(ver))
            {
                var dialog = new Dialog("Unsupported configuration", "This feature requires the WebView2 runtime to be installed on your computer.", SystemIcons.Information);
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

            AerochatLoginStatus loginStatus = login.Succeeded
                ? await app.BeginLogin(login.Token, rememberMe, status)
                : AerochatLoginStatus.UnknownFailure;

            if (loginStatus != AerochatLoginStatus.Success)
            {
                ViewModel.NotLoggingIn = true;

                var dialog = new Dialog("We can't sign you in to Discord", new List<Inline>() {
                    new Run("An error occurred attempting to use Discord password login. "),
                    new Hyperlink(new Run("For help retrieving your Discord token, click here."))
                    {
                        NavigateUri = new Uri(HELP_GET_TOKEN_URI)
                    },
                }, SystemIcons.Information);

                dialog.Owner = this;
                dialog.ShowDialog();
            }
        }

        private void PART_GetHelpLoggingInHyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(HELP_LOGON_URI) { UseShellExecute = true });
            e.Handled = true;
        }

        private void OnClickResetPasswordLink(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Dialog dialog = new("Reset password",
                new List<Inline>
                {
                    new Run("You cannot reset your Discord password from the Aerochat client. "),
                    new Hyperlink(new Run("Please open the Discord website to change your password."))
                    {
                        NavigateUri = new Uri("https://discord.com/login")
                    }
                },
                SystemIcons.Information
            );
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        /// <summary>
        /// Ensure that we can send multiline input to the token input box without it being clipped
        /// to only the first line.
        /// </summary>
        private void OnPasteIntoTokenInputBox(object sender, DataObjectPastingEventArgs e)
        {
            bool isText = e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true);
            if (!isText)
                return;

            string? text = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string;
            if (string.IsNullOrEmpty(text))
                return;

            text = text.Replace("\r", " ").Replace("\n", " ");

            DataObject d = new DataObject();
            d.SetData(DataFormats.Text, text);
            e.DataObject = d;
        }
    }
}
