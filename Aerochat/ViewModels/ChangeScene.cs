using Aerochat.Theme;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.ViewModels
{
    public class ChangeSceneItemViewModel : ViewModelBase
    {
        private SceneViewModel _scene;
        private bool _selected;
        public SceneViewModel Scene
        {
            get => _scene;
            set => SetProperty(ref _scene, value);
        }
        public bool Selected
        {
            get => _selected;
            set => SetProperty(ref _selected, value);
        }
    }
    public class ChangeSceneViewModel : ViewModelBase
    {
        public ObservableCollection<ChangeSceneItemViewModel> Scenes { get; } = new();
    }
}
