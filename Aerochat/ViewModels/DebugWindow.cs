using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.ViewModels
{
    public class DebugWindowViewModel : ViewModelBase
    {
        public ObservableCollection<AeroboolTreeItemViewModel> AeroboolTreeItems { get; } = [];
    }
}
