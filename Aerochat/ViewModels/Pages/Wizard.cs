using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.ViewModels
{
    public enum WizardButtonType
    {
        CommandLink,
        TextInput,
        BottomButton
    }
    public class WizardButtonViewModel : ViewModelBase
    {
        private string _text = "";
        private string _subtext = "";
        private int _height = 41;
        private Action? _action;
        private WizardButtonType _type = WizardButtonType.CommandLink;

        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }
        public string Subtext
        {
            get => _subtext ?? "";
            set => SetProperty(ref _subtext, value);
        }
        public Action? Action
        {
            get => _action;
            set => SetProperty(ref _action, value);
        }
        public int Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }
        public WizardButtonType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }
    }   
    public class WizardViewModel : ViewModelBase
    {
        private string _title = "";
        private string _description = "";
        
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

        public ObservableCollection<WizardButtonViewModel> Buttons { get; set; } = new();
    }
}
