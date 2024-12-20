using Aerochat.Hoarder;
using Aerochat.Settings;
using Aerochat.ViewModels;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
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
        public AeroboolTreeItemViewModel? CurrentlyDraggingItem;
        public DebugWindowViewModel ViewModel { get; } = new DebugWindowViewModel();
        public DebugWindow()
        {
            InitializeComponent();
            DataContext = ViewModel;

            StatusesComboBox.SelectionChanged += StatusesComboBox_SelectionChanged;
            // default to Online
            StatusesComboBox.SelectedItem = UserStatus.Online;

            var attachmentsEditor = PART_AttachmentsEditor.ViewModel;

            //attachmentsEditor.Attachments.Add(new(attachmentsEditor)
            //{
            //    FileName = "hello"
            //});
            //attachmentsEditor.Attachments.Add(new(attachmentsEditor)
            //{
            //    FileName = "hello2"
            //});
            //attachmentsEditor.Attachments.Add(new(attachmentsEditor)
            //{
            //    FileName = "hello3"
            //});
            //attachmentsEditor.Attachments.Add(new(attachmentsEditor)
            //{
            //    FileName = "hello4"
            //});
            //attachmentsEditor.Attachments.Add(new(attachmentsEditor)
            //{
            //    FileName = "hello5"
            //});
            //attachmentsEditor.Attachments.Add(new(attachmentsEditor)
            //{
            //    FileName = "hello6"
            //});
            //attachmentsEditor.Attachments.Add(new(attachmentsEditor)
            //{
            //    FileName = "hello7"
            //});
        }

        private void StatusesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.UserStatus = (UserStatus)StatusesComboBox.SelectedItem;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("hi");

            PART_AttachmentsEditor.ViewModel.Horizontal =
                !PART_AttachmentsEditor.ViewModel.Horizontal;

            Debug.WriteLine("orientation is now: " + (PART_AttachmentsEditor.ViewModel.Horizontal ? "horizontal" : "vertical"));
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            PART_AttachmentsEditor.ViewModel.AddItemsFromFilePicker();
        }
    }
}
