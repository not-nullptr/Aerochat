using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Interaction logic for DiscordLoginWV2.xaml
    /// </summary>
    public partial class DiscordLoginWV2 : Window
    {
        public string Token { get; set; }
        public DiscordLoginWV2()
        {
            InitializeComponent();
            OnLoad();
        }

        public async void OnLoad()
        {
            await LoginWebView.EnsureCoreWebView2Async();
            var coreWebView = LoginWebView.CoreWebView2;
            coreWebView.WebMessageReceived += CoreWebView_WebMessageReceived;
            await coreWebView.Profile.ClearBrowsingDataAsync();
            // remove ALL site 
            // load https://discord.com/login
            if (coreWebView is null) return;
            LoginWebView.Source = new Uri("https://discord.com/login");
            // inject js alert("Hello, World!");
            string script = @"
function onUrlChange() {
    window.chrome.webview.postMessage((webpackChunkdiscord_app.push([[''], {}, e => { m = []; for (let c in e.c) m.push(e.c[c]) }]), m).find(m => m?.exports?.default?.getToken !== void 0).exports.default.getToken());
}

const pushState = history.pushState;
history.pushState = function () {
    pushState.apply(history, arguments);
    onUrlChange();
};

const replaceState = history.replaceState;
history.replaceState = function () {
    replaceState.apply(history, arguments);
    onUrlChange();
};

window.addEventListener('popstate', onUrlChange);
window.addEventListener('hashchange', onUrlChange);
";
            await coreWebView.ExecuteScriptAsync(script);
        }

        private void CoreWebView_WebMessageReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            // this is the token - set it to Token and close the window
            Token = e.TryGetWebMessageAsString();
            Close();
        }
    }
}
