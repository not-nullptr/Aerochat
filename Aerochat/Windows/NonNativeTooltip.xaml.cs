using Aerochat.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Timer = System.Timers.Timer;

namespace Aerochat.Windows
{
    public class ItemClickedEventArgs(NonNativeItem item) : EventArgs
    {
        public NonNativeItem Item { get; set; } = item;
    }
    public partial class NonNativeTooltip : Window
    {
        public delegate void ItemClickedEventHandler(object sender, ItemClickedEventArgs e);
        public event ItemClickedEventHandler ItemClicked;
        public NonNativeTooltipViewModel ViewModel = new();

        private Timer _timer = new(100);

        public void StopKillTimer()
        {
            _timer.Stop();
        }

        public void StartKillTimer()
        {
            _timer.Start();
        }

        public async void RunOpenAnimation()
        {
            int steps = 5;
            int animationTime = 250;

            double startOpacity = 0;
            double endOpacity = 1;
            double opacityStepSize = (endOpacity - startOpacity) / steps;

            // Perform both top position and opacity animations
            for (int i = 0; i < steps; i++)
            {
                double opacity = startOpacity + opacityStepSize * i;

                await Dispatcher.InvokeAsync(() =>
                {
                    Opacity = opacity;
                });
                await Task.Delay(animationTime / steps);
            }

            await Dispatcher.InvokeAsync(() =>
            {
                Opacity = endOpacity;
            });
        }

        public async void RunCloseAnimation()
        {
            int steps = 5;
            int animationTime = 250;

            double startOpacity = 1;
            double endOpacity = 0;
            double opacityStepSize = (endOpacity - startOpacity) / steps;

            // Perform both top position and opacity animations
            for (int i = 0; i < steps; i++)
            {
                double opacity = startOpacity + opacityStepSize * i;

                await Dispatcher.InvokeAsync(() =>
                {
                    Opacity = opacity;
                });
                await Task.Delay(animationTime / steps);
            }

            await Dispatcher.InvokeAsync(() =>
            {
                Opacity = endOpacity;
                Close();
            });
        }


        public NonNativeTooltip(List<NonNativeItem> items)
        {
            _timer.AutoReset = false;
            Opacity = 0;
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.Items.Clear();
            foreach (var item in items)
                ViewModel.Items.Add(item);

            MouseEnter += (s, e) => StopKillTimer();
            MouseLeave += (s, e) => StartKillTimer();

            _timer.Elapsed += (s, e) => Dispatcher.Invoke(() => RunCloseAnimation());

            RunOpenAnimation();
        }

        public void OnItemClicked(object sender, RoutedEventArgs e)
        {
            NonNativeItem? item = (sender as Button)?.DataContext as NonNativeItem;
            if (item is null) return;
            ItemClicked?.Invoke(this, new ItemClickedEventArgs(item));
        }
    }
}
