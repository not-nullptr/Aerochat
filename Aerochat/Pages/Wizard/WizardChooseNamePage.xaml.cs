using Aerochat.Controls;
using Aerochat.Settings;
using Aerochat.ViewModels;
using Aerochat.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Aerochat.Pages.Wizard
{
    public partial class WizardChooseNamePage : Page
    {
        private CategoryWizard _window = null!;
        private bool IsServer;
        private string Lambda;
        public WizardChooseNamePage(bool isServer, string lambda)
        {
            InitializeComponent();
            Loaded += WizardPage_Loaded;
            IsServer = isServer;
            Lambda = lambda;
        }

        private void WizardPage_Loaded(object sender, RoutedEventArgs e)
        {
            _window = (CategoryWizard)Window.GetWindow(this);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _window.Close();
        }
        private void CommandLink_Loaded(object sender, RoutedEventArgs e)
        {
            var commandLink = (CommandLink)sender;
        }

        private void CommandLink_Click(object sender, EventArgs e)
        {
            var commandLink = (CommandLink)sender;
            if (commandLink is null) return;
            switch (commandLink.Tag)
            {
                case "Create":
                    _window.NavigateTo(new WizardServerOrDMPage());
                    break;
            }
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            if (Name.Text.Trim() == "") return;
            SettingsManager.Instance.CategoryLambdas.Add(new()
            {
                Name = Name.Text,
                Lambda = Lambda,
                Type = IsServer ? CategoryLambdaType.Guild : CategoryLambdaType.DM
            });
            SettingsManager.Save();
            _window.Close();
        }
    }
}
