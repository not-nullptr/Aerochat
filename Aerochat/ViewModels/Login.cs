using Aerochat.Theme;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.ViewModels
{
    public class LoginWindowViewModel : ViewModelBase
    {
        private SceneViewModel _scene;
        public SceneViewModel Scene
        {
            get => _scene;
            set => SetProperty(ref _scene, value);
        }

        private bool _notLoggingIn = true;
        public bool NotLoggingIn
        {
            get => _notLoggingIn;
            set => SetProperty(ref _notLoggingIn, value);
        }

        private bool _mfaRequired = false;
        public bool MFARequired
        {
            get => _mfaRequired;
            set => SetProperty(ref _mfaRequired, value);
        }

        private string _loginStatus = "Available";
        public string LoginStatus
        {
            get => _loginStatus;
            set => SetProperty(ref _loginStatus, value);
        }
    }
}
