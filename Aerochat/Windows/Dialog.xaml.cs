using Aerochat.ViewModels;
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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Aerochat.Windows
{
    public partial class Dialog : Window
    {
        public DialogViewModel ViewModel { get; set; } = new DialogViewModel();

        private Dialog(string title, Icon icon)
        {
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.Title = title;
            ViewModel.Icon = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            Loaded += Dialog_Loaded;
            AddHandler(Hyperlink.RequestNavigateEvent, new RoutedEventHandler(Hyperlink_RequestNavigate));
        }

        /// <summary>
        /// Construct a dialog containing a simple text description.
        /// </summary>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="description">A simple text string containing the description for this dialog.</param>
        /// <param name="icon">The icon to use for the dialog.</param>
        public Dialog(string title, string description, Icon icon)
            : this(title, icon)
        {
            ViewModel.Description = new() { new Run(description) };
            SetUpDescriptionView();
        }

        /// <summary>
        /// Construct a dialog containing a rich text description.
        /// </summary>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="description">A list of rich text content.</param>
        /// <param name="icon">The icon to use for the dialog.</param>
        public Dialog(string title, List<Inline> description, Icon icon)
            : this(title, icon)
        {
            ViewModel.Description = description;
            SetUpDescriptionView();
        }

        private void SetUpDescriptionView()
        {
            foreach (Inline inline in ViewModel.Description)
            {
                PART_Description.Inlines.Add(inline);
            }
        }

        private void Dialog_Loaded(object sender, RoutedEventArgs e)
        {
            bool hasOwner = Owner != null;

            if (hasOwner)
            {
                double dialogWidth = ActualWidth;
                double dialogHeight = ActualHeight;
                double ownerWidth = Owner.ActualWidth;
                double ownerHeight = Owner.ActualHeight;

                Left = Owner.Left + (ownerWidth - dialogWidth) / 2;
                Top = Owner.Top + (ownerHeight - dialogHeight) / 2;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RoutedEventArgs e)
        {
            if (e is RequestNavigateEventArgs request)
            {
                Process.Start(new ProcessStartInfo(request.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
        }
    }
}
