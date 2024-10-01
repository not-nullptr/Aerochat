using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;

#pragma warning disable CA1416

namespace Aerochat.Controls
{
    public enum EOpenOn
    { 
        LeftClick,
        RightClick,
        None
    }
    public class InteropMenuItem
    {
        public string Header { get; set; } = string.Empty;
        public ICommand? Command { get; set; }
        public List<InteropMenuItem> SubMenuItems { get; set; } = new List<InteropMenuItem>();

        public bool HasSubMenu => SubMenuItems.Count > 0;
    }
    public class InteropContextMenu : UserControl
    {
        private Dictionary<uint, int> IdToHashcodeMap = new();
        private HMENU _menu;
        private uint _ids = 0;

        public static readonly DependencyProperty ContextMenuItemsProperty =
            DependencyProperty.Register(nameof(ContextMenuItems), typeof(List<InteropMenuItem>), typeof(InteropContextMenu), new PropertyMetadata(new List<InteropMenuItem>(), null));

        public static readonly DependencyProperty XProperty =
            DependencyProperty.Register(nameof(X), typeof(int?), typeof(InteropContextMenu), new PropertyMetadata(null));

        public static readonly DependencyProperty YProperty =
            DependencyProperty.Register(nameof(Y), typeof(int?), typeof(InteropContextMenu), new PropertyMetadata(null));

        public static readonly DependencyProperty OpenOnProperty =
            DependencyProperty.Register(nameof(OpenOn), typeof(EOpenOn), typeof(InteropContextMenu), new PropertyMetadata(EOpenOn.RightClick));

        public static readonly DependencyProperty OpenToBottomProperty =
            DependencyProperty.Register(nameof(OpenToBottom), typeof(bool), typeof(InteropContextMenu), new PropertyMetadata(true));

        public static readonly DependencyPropertyKey IsOpenPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsOpen), typeof(bool), typeof(InteropContextMenu), new PropertyMetadata(false));

        public int? X
        {
            get => (int?)GetValue(XProperty);
            set => SetValue(XProperty, value);
        }

        public int? Y
        {
            get => (int?)GetValue(YProperty);
            set => SetValue(YProperty, value);
        }

        public EOpenOn OpenOn
        {
            get => (EOpenOn)GetValue(OpenOnProperty);
            set => SetValue(OpenOnProperty, value);
        }

        public bool OpenToBottom
        {
            get => (bool)GetValue(OpenToBottomProperty);
            set => SetValue(OpenToBottomProperty, value);
        }

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenPropertyKey.DependencyProperty);
            private set => SetValue(IsOpenPropertyKey, value);
        }

        public List<InteropMenuItem> ContextMenuItems
        {
            get => (List<InteropMenuItem>)GetValue(ContextMenuItemsProperty) ?? new List<InteropMenuItem>();
            set => SetValue(ContextMenuItemsProperty, value);
        }

        public InteropContextMenu()
        {
            PreviewMouseRightButtonUp += OnRightClick;
            PreviewMouseLeftButtonUp += OnLeftClick;
            Loaded += InteropContextMenu_Loaded;
        }

        private void OnLeftClick(object sender, MouseButtonEventArgs e)
        {
            if (OpenOn == EOpenOn.LeftClick)
            {
                Open();
            }
        }

        private void InteropContextMenu_Loaded(object sender, RoutedEventArgs e)
        {
            var wnd = Window.GetWindow(this);
            if (wnd is null) return; 
            wnd.Closing += InteropContextMenu_Closing;
        }

        private void InteropContextMenu_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            DestroyMenu(_menu);
            ContextMenuItems.Clear();
        }

        public static InteropMenuItem? FindHashcode(List<InteropMenuItem> items, int hashcode)
        {
            foreach (var item in items)
            {
                if (item.GetHashCode() == hashcode)
                {
                    return item;
                }
                if (item.HasSubMenu)
                {
                    var subItem = FindHashcode(item.SubMenuItems, hashcode);
                    if (subItem != null)
                    {
                        return subItem;
                    }
                }
            }
            return null;
        }


        // we need to manually import TrackPopupMenu
        [DllImport("user32.dll")]
        private static extern int TrackPopupMenu(IntPtr hMenu, TrackPopupMenuFlags uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

        private void OnRightClick(object sender, MouseButtonEventArgs e)
        {
            if (OpenOn == EOpenOn.RightClick)
            {
                Open();
            }
        }

        public void Open()
        {
            if (OpenToBottom && Content is not null)
            {
                var content = (FrameworkElement)Content;
                var pos = content.PointToScreen(new Point(0, 0));
                X = (int)pos.X;
                Y = (int)pos.Y + (int)content.ActualHeight;
            }
            _ids = 0;
            IdToHashcodeMap.Clear();

            if (_menu != default)
            {
                DestroyMenu(_menu);
                _menu = default;
                Debug.WriteLine("Destroyed menu");
            }
            var wnd = Window.GetWindow(this);
            if (wnd is null) return;
            IsOpen = true;
            var hWnd = new WindowInteropHelper(wnd).Handle;
            _menu = CreatePopupMenu();
            PopulateMenu(_menu, ContextMenuItems);
            //GetCursorPos(out POINT point);
            POINT point = new POINT();
            if (X.HasValue && Y.HasValue)
            {
                point.x = X.Value;
                point.y = Y.Value;
            }
            else
            {
                GetCursorPos(out point);
            }
            int res = TrackPopupMenu(_menu.DangerousGetHandle(), TrackPopupMenuFlags.TPM_LEFTALIGN | TrackPopupMenuFlags.TPM_RETURNCMD, point.x, point.y, 0, hWnd, default);
            if (IdToHashcodeMap.TryGetValue((uint)res, out int hashcode))
            {
                var item = FindHashcode(ContextMenuItems, hashcode);
                item?.Command?.Execute(null);
            }
            IsOpen = false;
        }

        public void Close()
        {
            if (_menu == default) return;
            DestroyMenu(_menu);
            _menu = default;
            IsOpen = false;
        }

        public void PopulateMenu(HMENU menu, List<InteropMenuItem> contextMenuItems)
        {
            foreach (var item in contextMenuItems)
            {
                _ids++;
                if (item.HasSubMenu)
                {
                    var subMenu = CreatePopupMenu();
                    AppendMenu(menu, MenuFlags.MF_STRING | MenuFlags.MF_POPUP, subMenu.DangerousGetHandle(), item.Header);
                    PopulateMenu(subMenu, item.SubMenuItems);
                }
                else
                {
                    IdToHashcodeMap.Add(_ids, item.GetHashCode());
                    AppendMenu(menu, MenuFlags.MF_STRING, (nint)_ids, item.Header);
                }
            }
        }
    }
}
