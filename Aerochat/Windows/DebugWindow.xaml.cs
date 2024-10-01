using Aerochat.Hoarder;
using Aerochat.Settings;
using Aerochat.ViewModels;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Aerochat.Windows
{
    /// <summary>
    /// Interaction logic for DebugWindow.xaml
    /// </summary>
    public partial class DebugWindow : Window
    {
        public AeroboolTreeItemViewModel? CurrentlyDraggingItem;
        public DebugWindowViewModel ViewModel { get; } = new DebugWindowViewModel();
        public DebugWindow()
        {
            InitializeComponent();
            SourceTreeView.MouseMove += SourceTreeView_MouseMove;
            TargetTreeView.Drop += TargetTreeView_Drop;
            DataContext = ViewModel;
            var classes = typeof(DiscordChannel).Assembly.GetTypes().Where(x => x.Namespace == "DSharpPlus.Entities" && x.IsSubclassOf(typeof(SnowflakeObject))).ToList();
            ViewModel.AeroboolTreeItems.Add(new()
            {
                Name = "Discord Types",
                Type = "/Resources/BoolEditor/Namespace.png"
            });
            foreach (var @class in classes)
            {
                ViewModel.AeroboolTreeItems[0].Children.Add(new()
                {
                    Name = @class.Name,
                    Type = "/Resources/BoolEditor/Class.png",
                    Value = @class
                });
            }
            ViewModel.AeroboolTreeItems.Add(new()
            {
                Name = "Primitive Types",
                Type = "/Resources/BoolEditor/Namespace.png"
            });
            List<Type> primitives = new()
            {
                typeof(string),
                typeof(bool),
                typeof(int),
                typeof(char),
                typeof(long),
                typeof(float),
                typeof(double),
                typeof(decimal),
                typeof(byte),
                typeof(sbyte),
                typeof(short),
                typeof(ushort),
                typeof(uint),
                typeof(ulong)
            };

            foreach (var primitive in primitives)
            {
                ViewModel.AeroboolTreeItems[1].Children.Add(new()
                {
                    Name = primitive.Name,
                    Type = "/Resources/BoolEditor/Class.png",
                    Value = primitive
                });
            }
        }

        private void TargetTreeView_Drop(object sender, DragEventArgs e)
        {
            // Check if the data being dragged is of the expected type
            if (e.Data.GetDataPresent("Object"))
            {
                // Retrieve the dragged item
                var droppedItem = e.Data.GetData("Object") as AeroboolTreeItemViewModel;

                if (droppedItem != null)
                {
                    AeroboolTreeItemViewModel? FindParent(AeroboolTreeItemViewModel item)
                    {
                        AeroboolTreeItemViewModel? FindParentInternal(AeroboolTreeItemViewModel parent)
                        {
                            if (parent.Children.Contains(item))
                                return parent;
                            foreach (var child in parent.Children)
                            {
                                var result = FindParentInternal(child);
                                if (result != null)
                                    return result;
                            }
                            return null;
                        }

                        foreach (var child in ViewModel.AeroboolTreeItems)
                        {
                            var result = FindParentInternal(child);
                            if (result != null)
                                return result;
                        }
                        return null;
                    }
                    var parent = FindParent(droppedItem);
                    if (parent != null)
                        parent.Children.Remove(droppedItem);
                    else if (ViewModel.AeroboolTreeItems.Contains(droppedItem))
                        ViewModel.AeroboolTreeItems.Remove(droppedItem);
                }
            }

            CurrentlyDraggingItem = null;
        }


        private void SourceTreeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                CurrentlyDraggingItem = null;
            }
            if (e.LeftButton != MouseButtonState.Pressed || CurrentlyDraggingItem != null || SourceTreeView.SelectedItem is null)
                return;

            // get the currently selected item
            var selected = (AeroboolTreeItemViewModel)SourceTreeView.SelectedItem;
            // get the treeviewitem recursively
            if (selected != null) {
                var treeViewItem = SourceTreeView.ItemContainerGenerator.ContainerFromItem(selected) as TreeViewItem;
                if (treeViewItem == null)
                {
                    treeViewItem = SourceTreeView.ItemContainerGenerator.ContainerFromItem(selected) as TreeViewItem;
                }
                if (treeViewItem == null)
                    return;
            }

            CurrentlyDraggingItem = selected;

            // initialize the drag & drop operation
            DataObject dragData = new();
            dragData.SetData("Object", CurrentlyDraggingItem);
            DragDrop.DoDragDrop(SourceTreeView, dragData, DragDropEffects.Move);
        }
    }

    public class TreeViewLineConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TreeViewItem item = (TreeViewItem)value;
            ItemsControl ic = ItemsControl.ItemsControlFromItemContainer(item);
            return ic.ItemContainerGenerator.IndexFromContainer(item) == ic.Items.Count - 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return false;
        }
    }
}
