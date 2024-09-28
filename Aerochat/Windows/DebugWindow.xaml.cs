using Aerochat.Hoarder;
using Aerochat.Settings;
using Aerochat.ViewModels;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Interaction logic for DebugWindow.xaml
    /// </summary>
    public partial class DebugWindow : Window
    {
        public DebugWindowViewModel ViewModel { get; } = new DebugWindowViewModel();
        public DebugWindow()
        {
            InitializeComponent();
            // add all UserStatuses to StatusesComboBox
            StatusesComboBox.ItemsSource = Enum.GetValues(typeof(UserStatus)).Cast<UserStatus>();
            DataContext = ViewModel;
            StatusesComboBox.SelectionChanged += StatusesComboBox_SelectionChanged;
            // default to Online
            StatusesComboBox.SelectedItem = UserStatus.Online;
        }

        private void StatusesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.UserStatus = (UserStatus)StatusesComboBox.SelectedItem;
        }

        private void ClearWarnings_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Instance.WarningShown = false;
            SettingsManager.Instance.HasWarnedAboutVoiceChat = false;
            SettingsManager.Instance.ViewedNotices.Clear();
            SettingsManager.Save();
        }

        private void MakeNoti_Click(object sender, RoutedEventArgs e)
        {
             new Notification(NotificationType.SignOn, new
             {
                 User = Discord.Client.CurrentUser,
                 Presence = Discord.Client.Presences.First().Value
             }).Show();
        }
    }
}
