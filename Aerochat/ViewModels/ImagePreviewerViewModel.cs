using Aerochat.Enums;

namespace Aerochat.ViewModels
{
    public sealed class ImagePreviewerViewModel: ViewModelBase
    {
        private string _filename;
        private string _sourceUri;
        private int _bottomHeight;
        public MediaType _mediaType;

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

        public MediaType MediaType
        {
            get => _mediaType;
            set => SetProperty(ref _mediaType, value);
        }
    }
}
