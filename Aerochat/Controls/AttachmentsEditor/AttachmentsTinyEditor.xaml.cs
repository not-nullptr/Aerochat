using Aerochat.ViewModels;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Aerochat.Controls.AttachmentsEditor
{
    /// <summary>
    /// Interaction logic for AttachmentsTinyEditor.xaml
    /// </summary>
    public partial class AttachmentsTinyEditor : UserControl
    {
        public AttachmentsTinyEditor()
        {
            InitializeComponent();

            PreviewKeyDown += OnKeyDown;
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var item = DataContext as ViewModels.AttachmentsEditorItem;
            item?.Remove();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.Enter)
            {
                Popup? element = sender as Popup;

                if (element != null)
                {
                    element.IsOpen = false;
                }
            }
        }
    }
}
