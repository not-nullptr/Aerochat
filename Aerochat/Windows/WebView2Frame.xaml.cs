using DSharpPlus.Entities;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
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
    public partial class WebView2Frame : Window
    {
        private readonly DiscordCaptchaRequest captchaRequest;

        public DiscordCaptchaResponse CaptchaResponse;

        public WebView2Frame(DiscordCaptchaRequest captchaRequest)
        {
            this.captchaRequest = captchaRequest;
            InitializeComponent();

            OnLoad();
        }

        public async void OnLoad()
        {
            await CaptchaWebView.EnsureCoreWebView2Async(); // ensure the CoreWebView2 is created
            var coreWebView = CaptchaWebView.CoreWebView2;
            if (coreWebView != null)
            {
                // use SetVirtualHostNameToFolderMapping to load the hCaptcha.html file from WebDir relative to the assembly

                var path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "WebDir") ?? "";
                coreWebView.SetVirtualHostNameToFolderMapping("wpfresources", path, CoreWebView2HostResourceAccessKind.DenyCors);

                CaptchaWebView.Source = new Uri($"https://wpfresources/hCaptcha.html?siteKey={captchaRequest.SiteKey}");

                CaptchaWebView.WebMessageReceived += OnCaptchaWebMessageReceived;
            }
        }

        private async void OnCaptchaWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            var jObject = JObject.Parse(args.WebMessageAsJson);
            await Dispatcher.InvokeAsync(() =>
            {
                var op = jObject["op"].ToObject<string>();
                switch (op)
                {
                    case "captcha_complete":
                        CaptchaResponse = new DiscordCaptchaResponse(jObject["token"].ToObject<string>());
                        break;
                };
            });
        }
    }
}
