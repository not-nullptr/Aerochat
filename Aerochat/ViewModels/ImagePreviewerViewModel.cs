using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Aerochat.ViewModels
{
    public sealed class ImagePreviewerViewModel: ViewModelBase
    {
        private string _filename;
        private string _sourceUri;
        private int _bottomHeight;

        public string FileName
        {
            get => _filename;
            set => SetProperty(ref _filename, value);
        }

        public string SourceUri
        {
            get => _sourceUri;
            set => SetProperty(ref _sourceUri, value);
        }

        public int BottomHeight
        {
            get => _bottomHeight;
            set => SetProperty(ref _bottomHeight, value);
        }
    }
}
