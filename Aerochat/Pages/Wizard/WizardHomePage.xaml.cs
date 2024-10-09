using Aerochat.Controls;
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
    public partial class WizardHomePage : Page
    {
        private CategoryWizard _window = null!;
        public WizardHomePage()
        {
            InitializeComponent();
            Loaded += WizardPage_Loaded;
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
    }
}
