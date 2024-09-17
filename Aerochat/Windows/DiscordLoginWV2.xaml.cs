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
function onRes(res) {
    if (res.captcha_key?.includes(""captcha-required"")) return;
    if (!res.token) return;
    window.chrome.webview.postMessage(res.token);
}

(function (send) {
  XMLHttpRequest.prototype.send = function (data) {
    console.log('Request', this);

    const url = new URL(this.__sentry_xhr_v3__.url);
    if (url.pathname === ""/api/v9/auth/login"" || url.pathname === ""/api/v9/auth/mfa/totp"") {
    if (this.responseText) {
        onRes(JSON.parse(this.responseText));
    } else {
        this.addEventListener(""readystatechange"", function () {
            onRes(JSON.parse(this.responseText));
        });
    }
    }

    send.call(this, data);
  };
})(XMLHttpRequest.prototype.send);

function clearStorage() {
    localStorage.clear();
    sessionStorage.clear();
    // all cookies
    document.cookie.split(';').forEach(function(c) {
        document.cookie = c.replace(/^ +/, '').replace(/=.*/, '=;expires=' + new Date().toUTCString() + ';path=/');
    });
}

clearStorage();

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
