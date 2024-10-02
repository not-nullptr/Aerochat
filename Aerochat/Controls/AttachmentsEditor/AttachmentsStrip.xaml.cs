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

            //Window w = Window.GetWindow(myPopupPlacementTarget);
            //if (null != w)
            //{
            //    w.LocationChanged += delegate (object sender, EventArgs args)
            //    {
            //        var offset = myPopup.HorizontalOffset;
            //        myPopup.HorizontalOffset = offset + 1;
            //        myPopup.HorizontalOffset = offset;
            //    };
            //}
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            AttachmentsTinyEditor editorWindow = new();
            
        }
    }
}
