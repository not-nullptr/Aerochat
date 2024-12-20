using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private string _localFileName = "";

        public string LocalFileName
        {
            get => _localFileName;
            set => SetProperty(ref _localFileName, value);
        }

        private string _fileName = "";

        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
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
    }
}
