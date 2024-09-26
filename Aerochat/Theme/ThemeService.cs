using Aerochat.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Aerochat.Theme
{
    public class ThemeService : ViewModelBase
    {
        private SceneViewModel _scene;

        public SceneViewModel Scene
        {
            get => _scene;
            set => SetProperty(ref _scene, value);
        }

        public ObservableCollection<SceneViewModel> Scenes { get; } = new();

        public static ThemeService Instance { get; } = new();
    }
}
