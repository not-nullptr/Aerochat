using Aerochat.Theme;
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
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();
            // get all scenes
            var scenes = ThemeService.Instance.Scenes;
            string credits = "Aerochat is a project by nullptr. Most assets belong to Microsoft, please don't sue!\n\n";
            foreach (var scene in scenes)
            {
                credits += $"\"{scene.DisplayName}\" was made by {scene.Credit}\n";
            }

            CreditsTextbox.Text = credits;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
