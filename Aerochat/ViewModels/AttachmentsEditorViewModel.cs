using Aerochat.Helpers.AttachmentEditor;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Vanara.PInvoke;

namespace Aerochat.ViewModels
{
    public class AttachmentsEditorViewModel : ViewModelBase
    {
        private bool _horizontal = true;

        /// <summary>
        /// Controls the orientation of the attachments editor.
        /// </summary>
        public bool Horizontal
        {
            get => _horizontal;
            set
            {
                SetProperty(ref _horizontal, value);
                InvokePropertyChanged("HorizontalScrollbarVisibility");
                InvokePropertyChanged("VerticalScrollbarVisibility");
                InvokePropertyChanged("ItemViewOrientation");
            }
        }

        public ScrollBarVisibility HorizontalScrollbarVisibility
        {
            get
            {
                if (Horizontal)
                {
                    return ScrollBarVisibility.Auto;
                }

                return ScrollBarVisibility.Disabled;
            }

            private set { }
        }

        public ScrollBarVisibility VerticalScrollbarVisibility
        {
            get
            {
                if (!Horizontal)
                {
                    return ScrollBarVisibility.Auto;
                }

                return ScrollBarVisibility.Disabled;
            }

            private set { }
        }

        public Orientation ItemViewOrientation
        {
            get
            {
                return Horizontal ? Orientation.Horizontal : Orientation.Vertical;
            }

            private set { }
        }

        public ObservableCollection<AttachmentsEditorItem> Attachments { get; set; } = new();

        public void AddItemsFromFilePicker()
        {
            Microsoft.Win32.OpenFileDialog dialog = new();
            dialog.Multiselect = true;

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                foreach (string fileName in dialog.FileNames)
                {
                    Application.Current.Dispatcher.Invoke(() => AddItem(fileName));
                }
            }
        }

        public void AddItem(string fileName)
        {
            string baseName = Path.GetFileName(fileName);

            AttachmentsEditorItem item = new(this)
            {
                LocalFileName = fileName,
                FileName = baseName,
                Selected = true,
            };

            try
            {
                long fileSize = new FileInfo(fileName).Length;
                string formattedFileSize = FormatSize(fileSize);

                item.FileSize = formattedFileSize;
            }
            catch { /* ignore */ }

            if (!IsOfImageFileType(baseName))
            {
                ProgramIconManager iconManager = new();
                BitmapSource aaa = iconManager.LoadIcon(fileName);

                if (aaa == null)
                {
                    System.Diagnostics.Debug.WriteLine("Loaded null icon for " + fileName);
                }

                item.BitmapSource = aaa;
                item.IsImage = false;
            }
            else
            {
                item.IsImage = true;

                BitmapImage image = new();
                image.BeginInit();
                image.DecodePixelHeight = 48;
                image.UriSource = new Uri(fileName);
                image.EndInit();

                item.BitmapSource = image;
            }

            Attachments.Add(item);
        }

        internal bool IsOfImageFileType(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();

            System.Diagnostics.Debug.WriteLine("extension: " + extension);

            return extension switch
            {
                ".png" => true,
                ".jpg" => true,
                ".jpeg" => true,
                ".webp" => true,
                ".gif" => true,
                ".bmp" => true,
                _ => false,
            };
        }

        // TODO: This is copied between Attachment.cs and here. Move it to somewhere common.
        private static string FormatSize(long size)
        {
            return size switch
            {
                < 1024 => $"{size} B",
                < 1024 * 1024 => $"{size / 1024} KB",
                < 1024 * 1024 * 1024 => $"{size / 1024 / 1024} MB",
                _ => $"{size / 1024 / 1024 / 1024} GB"
            };
        }
    }
}
