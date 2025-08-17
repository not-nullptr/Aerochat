using Aerochat.Theme;
using Aerochat.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;
using System.Xml.Linq;

namespace Aerochat.Windows
{
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();

            PART_AerochatVersion.Text = "Aerochat version " + Assembly.GetExecutingAssembly().GetName().Version!.ToString(3)
#if AEROCHAT_RC
                + " " + AssemblyInfo.RC_REVISION
#endif
                + "\n";

            string credits = "Aerochat is a project by nullptr. Most assets belong to Microsoft, please don't sue!\n\n";

            // Miscellaneous resource credits:
            credits += "==== Credits for resources used in Aerochat ==== \n\n";
            credits += "The delete icon in the file attachments editor was made by Laserman.\n";
            credits += "\n\n";

            // Get all scenes:
            credits += "==== Credits for scenes ==== \n\n";
            var scenes = ThemeService.Instance.Scenes;
            foreach (var scene in scenes)
            {
                credits += $"\"{scene.DisplayName}\" was made by {scene.Credit}\n";
            }
            credits += "\n\n";

            // Get all ads:
            // TODO: Refactor into an ads service. Out of laziness, I just copied this code from the home page
            // view model.
            List<XElement> ads = new();

            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = "Aerochat.Ads.Ads.xml";
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new(stream);
            string result = reader.ReadToEnd();
            XDocument doc = XDocument.Parse(result);
            foreach (XElement adXml in doc.Root?.Elements() ?? Enumerable.Empty<XElement>())
            {
                ads.Add(adXml);
            }

            credits += "==== Credits for ads ==== \n\n";
            foreach (var ad in ads) if (ad.Attribute("Image") is not null)
            {
                credits += $"\"{ad.Attribute("Image")?.Value}\" was made by {ad.Attribute("Submitter")?.Value ?? "an unknown author. Please contact us if you made this ad."}\n";
            }
            // This is the group, so do not put the newlines.
            //credits += "\n\n";

            CreditsTextbox.Text = credits;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
