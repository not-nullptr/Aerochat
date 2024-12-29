using Aerochat.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Aerochat.Controls.AttachmentsEditor
{
    public class StripItemTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement? containerEl = container as FrameworkElement;

            if (containerEl == null)
            {
                return null;
            }

            if (item == null || !(item is AttachmentsEditorItem))
            {
                throw new ApplicationException("Wrong type");
            }

            if ((item as AttachmentsEditorItem).IsImage)
            {
                return containerEl.FindResource("ImageItemContent") as DataTemplate;
            }
            else
            {
                return containerEl.FindResource("FileItemContent") as DataTemplate;
            }

            throw new ApplicationException("Failed to select valid template.");
        }
    }
}
