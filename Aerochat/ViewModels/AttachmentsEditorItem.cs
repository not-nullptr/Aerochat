using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Aerochat.ViewModels
{
    public class AttachmentsEditorItem : ViewModelBase
    {
        private AttachmentsEditorViewModel _parent;

        public AttachmentsEditorItem(AttachmentsEditorViewModel parent)
        {
            _parent = parent;
        }

        private string? _localFileName = null;

        public string? LocalFileName
        {
            get => _localFileName;
            set => SetProperty(ref _localFileName, value);
        }

        private bool _isVirtual = false;

        public bool IsVirtual
        {
            get => _isVirtual;
            set => SetProperty(ref _isVirtual, value);
        }

        private Stream? _virtualStream = null;

        public Stream? VirtualStream
        {
            get => _virtualStream;
            set => SetProperty(ref _virtualStream, value);
        }

        private string _fileName = "";

        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        private string _accessibilityText = "";

        public string AccessibilityText
        {
            get => _accessibilityText;
            set => SetProperty(ref _accessibilityText, value);
        }

        private bool _markAsSpoiler = false;

        public bool MarkAsSpoiler
        {
            get => _markAsSpoiler;
            set => SetProperty(ref _markAsSpoiler, value);
        }

        private string? _fileSize = null;

        public string? FileSize
        {
            get => _fileSize;
            set => SetProperty(ref _fileSize, value);
        }

        private bool _selected = false;

        public bool Selected
        {
            get => _selected;
            set => SetProperty(ref _selected, value);
        }

        public bool IsImage
        {
            get
            {
                return _parent.IsOfImageFileType(Path.GetExtension(FileName));
            }
            set { }
        }

        private BitmapSource? _bitmapSource = null;

        public BitmapSource? BitmapSource
        {
            get => _bitmapSource;
            set => SetProperty(ref _bitmapSource, value);
        }

        public Stream GetStream()
        {
            if (_isVirtual)
            {
                if (_virtualStream == null)
                {
                    throw new Exception("Stream should not be null.");
                }

                return _virtualStream;
            }
            else
            {
                if (LocalFileName == null)
                {
                    throw new Exception("Local file name should not be null.");
                }

                return File.OpenRead(LocalFileName);
            }
        }

        public void Remove()
        {
            _parent.Attachments.Remove(this);
        }
    }
}
