using Aerochat.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Aerochat.Controls.AttachmentsEditor
{
    /// <summary>
    /// Interaction logic for AttachmentsStrip.xaml
    /// </summary>
    public partial class AttachmentsStrip : UserControl
    {
        public AttachmentsEditorViewModel ViewModel = new();

        public AttachmentsStrip()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            AttachmentsEditorItem? currentItem = null;

            if (sender is FrameworkElement element)
            {
                if (element.DataContext is AttachmentsEditorItem item)
                {
                    currentItem = item;
                }
            }

            foreach (var attachment in ViewModel.Attachments)
            {
                if (attachment != currentItem)
                {
                    attachment.Selected = false;
                }
            }
        }

        private void ItemMiniEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            Popup? element = sender as Popup;

            if (element != null)
            {
                element.IsOpen = false;
            }
        }
    }
}
