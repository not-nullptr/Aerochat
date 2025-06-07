using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace Aerochat.ViewModels
{
    public class DialogViewModel : ViewModelBase
    {
        private string _title;
        private List<Inline> _description;
        private BitmapSource _icon;
        
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public List<Inline> Description
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
