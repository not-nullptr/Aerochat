using Aerochat.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.Helpers.AttachmentEditor
{
    public class AttachmentEditorManager
    {
        private AttachmentsEditorViewModel _viewModel;

        public AttachmentEditorManager()
        {
            _viewModel = new();
        }

        public AttachmentsEditorViewModel ViewModel
        {
            get => _viewModel;
            private set { }
        }
    }
}
