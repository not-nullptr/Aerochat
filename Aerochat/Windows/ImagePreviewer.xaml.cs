﻿using Aerochat.Enums;
using Aerochat.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using XamlAnimatedGif;
using static Vanara.PInvoke.DwmApi;

namespace Aerochat.Windows
{
    /// <summary>
    /// Interaction logic for ImagePreviewer.xaml
    /// </summary>
    public partial class ImagePreviewer : Window
    {
        private bool _finished = false;
        private bool _closing = false;
        public ImagePreviewerViewModel ViewModel { get; private set; }

        public ImagePreviewer(AttachmentViewModel attachmentVm)
        {
            ViewModel = new ImagePreviewerViewModel
            {
                FileName = attachmentVm.Name,
                SourceUri = attachmentVm.MediaType == Enums.MediaType.Video ? "" : attachmentVm.Url,
                BottomHeight = 40,
                MediaType = attachmentVm.MediaType
            };

            DataContext = ViewModel;
            InitializeComponent();
        }

        public ImagePreviewer(EmbedImageViewModel embedImageVm)
        {
            ViewModel = new ImagePreviewerViewModel
            {
                FileName = embedImageVm.Url,
                SourceUri = embedImageVm.Url,
                BottomHeight = 40,
                MediaType = MediaType.Image
            };

            DataContext = ViewModel;
            InitializeComponent();
        }

        private void OnImagePreviewSizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        private void OnImagePreviewLoaded(object sender, RoutedEventArgs e)
        {
            // Without this, resizing the image viewer makes the app freak out and hang
            if (ViewModel.MediaType == MediaType.Gif)
                AnimationBehavior.SetCacheFramesInMemory(ImageElement, true);

            IntPtr mainWindowPtr = new WindowInteropHelper(this).Handle;
            HwndSource mainWindowSrc = HwndSource.FromHwnd(mainWindowPtr);
            mainWindowSrc.CompositionTarget.BackgroundColor = Color.FromArgb(0, 0, 0, 0);
            DwmExtendFrameIntoClientArea(mainWindowPtr, new MARGINS(0, 0, 0, ViewModel.BottomHeight));

            Task.Run(async () =>
            {
                await Task.Delay(10);
                Dispatcher.BeginInvoke(Focus);
                _finished = true;
            });
        }

        private void OnOpenImageClick(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ViewModel.SourceUri,
                UseShellExecute = true
            });
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;
            if (_closing) return;

            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                _closing = true;
                Close();
            }
        }

        private void OnDeactivated(object sender, EventArgs e)
        {
            // if we're already closing, don't close again
            if (!_finished || _closing) return;
            _closing = true;
            Close();
        }

        private void OnCloseBtnClick(object sender, RoutedEventArgs e)
        {
            if (!_finished || _closing) return;
            _closing = true;
            Close();
        }

        private bool _ranClose = false;

        // TODO: Fix closing animation for gifs
        private void OnImagePreviewClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ViewModel.MediaType == MediaType.Gif)
                AnimationBehavior.SetCacheFramesInMemory(ImageElement, false);

            //Owner.Focus();

            //if (_ranClose) return;
            //_ranClose = true;
            //e.Cancel = true;
            //// focus the owner
            ////Owner.Focus();
            //var duration = TimeSpan.FromSeconds(0.5);
            //var easingFunction = new QuinticEase { EasingMode = EasingMode.EaseIn };

            //// animate from _dstRect to _srcRect
            //var leftAnimation = new DoubleAnimation
            //{
            //    From = _dstRect.Left,
            //    To = _srcRect.Left - 10,
            //    Duration = duration,
            //    EasingFunction = easingFunction
            //};
            //var topAnimation = new DoubleAnimation
            //{
            //    From = _dstRect.Top,
            //    To = _srcRect.Top + 40,
            //    Duration = duration,
            //    EasingFunction = easingFunction
            //};
            //var widthAnimation = new DoubleAnimation
            //{
            //    From = _dstRect.Width,
            //    To = _srcRect.Width,
            //    Duration = duration,
            //    EasingFunction = easingFunction
            //};
            //var heightAnimation = new DoubleAnimation
            //{
            //    From = _dstRect.Height - 12,
            //    To = _srcRect.Height + 16,
            //    Duration = duration,
            //    EasingFunction = easingFunction
            //};

            //leftAnimation.Completed += (s, _) =>
            //{
            //    Close();
            //};

            //BeginAnimation(Window.LeftProperty, leftAnimation);
            //BeginAnimation(Window.TopProperty, topAnimation);
            //WndContent.BeginAnimation(Grid.WidthProperty, widthAnimation);
            //WndContent.BeginAnimation(Grid.HeightProperty, heightAnimation);
        }
    }
}