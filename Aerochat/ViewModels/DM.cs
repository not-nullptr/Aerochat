using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.ViewModels
{
    public class DMViewModel : ViewModelBase
    {
        private UserViewModel _user;
        private ChannelViewModel _channel;

        public UserViewModel User
        {
            get => _user;
            set => SetProperty(ref _user, value);
        }
        public ChannelViewModel Channel
        {
            get => _channel;
            set => SetProperty(ref _channel, value);
        }
    }
}
