using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.ViewModels
{
    public class DebugWindowViewModel : ViewModelBase
    {
        private UserStatus _userStatus = UserStatus.Online;
        public UserStatus UserStatus
        {
            get => _userStatus;
            set => SetProperty(ref _userStatus, value);
        }
    }
}
