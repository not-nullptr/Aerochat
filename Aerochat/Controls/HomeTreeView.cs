using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Aerochat.Controls
{
    /// <summary>
    /// Used for the tree view on the home page. Needed to support HomeTreeViewItem.
    /// </summary>
    public class HomeTreeView : TreeView
    {
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new HomeTreeViewItem();
        }
    }
    
    /// <summary>
    /// Tree view item which doesn't use native double-click expand/collapse view functionality.
    /// </summary>
    public class HomeTreeViewItem : TreeViewItem
    {
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            // Do nothing.
        }
    }
}
