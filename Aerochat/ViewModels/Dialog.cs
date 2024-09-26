using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Aerochat.ViewModels
{
    public class DialogViewModel : ViewModelBase
    {
        private string _title;
        private string _description;
        private BitmapSource _icon;
        
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }
        public BitmapSource Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }
    }
}
