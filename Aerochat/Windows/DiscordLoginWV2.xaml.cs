using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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

namespace Aerochat.Windows
{
    /// <summary>
    /// Interaction logic for DiscordLoginWV2.xaml
    /// </summary>
    public partial class DiscordLoginWV2 : Window
    {
        public const string INJECTED_JS_SCRIPT = @"
function tryGetDiscordTokenFromWebpackChunk() {
    const result = {
        status: false,
        token: '',
    };

    try {
        // I'm not sure what this code was originally for, but Maddie's original code
        // contained it. My guess is that it just ensures an entry exists for the find
        // call.
        let m = [];
        webpackChunkdiscord_app.push([ [''], {}, e => {
            m = [];
            for (let c in e.c)
                m.push(e.c[c]);
        } ], m);

        let fnGetToken = webpackChunkdiscord_app.find(m => m?.exports?.default?.getToken !== void 0)?.exports?.default?.getToken;

        if (fnGetToken)
        {
            result.token = fnGetToken();
            result.status = true;
        }
    }
    catch (e) {
        // Ignore.
    }

    return result;
}

function tryGetDiscordTokenFromIframeLocalStorage() {
    try {
        let iframe = document.createElement('iframe');
        document.body.appendChild(iframe);
        // \u0222 = double quotes
        let token = iframe.contentWindow.localStorage.token.replaceAll('\u0022', '');
        iframe.remove();
    
        if (token) {
            return {
                status: true,
                token: token
            };
        }
    }
    catch (e) {
        // Ignore.
    }

    return {
        status: false,
        token: ''
    };
}

function tryGetDiscordToken() {
    let result = tryGetDiscordTokenFromWebpackChunk();
    if (!result.status)
        result = tryGetDiscordTokenFromIframeLocalStorage();

    return result;
}

function serializeDiscordTokenForCSharp(result) {
    if (result.status == true) {
        return 'SUCCESS:' + result.token;
    }
    
    return 'FAILURE:NULL';
}

function onUrlChange() {
    window.chrome.webview.postMessage(serializeDiscordTokenForCSharp(tryGetDiscordToken()));
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

        private string _uid;


        public string Token { get; set; }
        public bool Succeeded { get; set; } = false;
        public DiscordLoginWV2()
        {
            _uid = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString();

            InitializeComponent();
            OnLoad();

            Closing += OnClosing;
        }

        private string GetProfileDirectory()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aerochat", "LogonWebViewData-" + _uid);
        }

        public async void OnLoad()
        {
            await LoginWebView.EnsureCoreWebView2Async(await CoreWebView2Environment.CreateAsync(
                null,
                GetProfileDirectory()
            ));

            var coreWebView = LoginWebView.CoreWebView2;

            if (coreWebView is null)
                return;

            // Make sure that all cookies and other information are cleared so we don't open to an
            // already-logged-in Discord session if the user logged in via this method before.
            await coreWebView.Profile.ClearBrowsingDataAsync();

            coreWebView.WebMessageReceived += CoreWebView_WebMessageReceived;

            LoginWebView.Source = new Uri("https://discord.com/login");

            // Inject the JS script so we can intercept login and get the user's token in order to
            // log in with it.
            await coreWebView.ExecuteScriptAsync(INJECTED_JS_SCRIPT);
        }

        private async void CoreWebView_WebMessageReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();

            string[] parts = message.Split(':');
            string statusCode = parts[0];
            string token = parts[1];

            // this is the token - set it to Token and close the window
            if (statusCode == "SUCCESS")
            {
                Token = token;
                Succeeded = true;
            }
            else // if (statusCode == "FAILURE") // or anything else
            {
                Succeeded = false;
                Hide();
                Dialog errorDialog = new(
                    "Error",
                    new List<Inline>
                    {
                        new Run("We failed to automatically extract the token via password logon. This probably means that Discord made " +
                                "a security change that prevents our script from doing its job. Please try to retrieve your token manually " +
                                "and log in using that.\n\n"),
                        new Hyperlink(new Run("Click here to get help logging into Aerochat."))
                        {
                            NavigateUri = new Uri(Login.HELP_LOGON_URI),
                        }
                    }
                    ,
                    SystemIcons.Error
                );
                errorDialog.Owner = this;
                errorDialog.ShowDialog();
            }

            Close();
        }

        private void OnClosing(object? sender, EventArgs e)
        {
            // Delete the WebView2 profile; the data will never be accessed again by Aerochat.
            LoginWebView?.CoreWebView2?.Profile?.Delete();

            uint pid = LoginWebView?.CoreWebView2?.BrowserProcessId ?? 0;
            LoginWebView?.Dispose();

            if (pid > 0)
            {
                Process process = Process.GetProcessById(Convert.ToInt32(pid));
                process.WaitForExit(10000);
            }

            try
            {
                Directory.Delete(GetProfileDirectory(), true);
            }
            catch (Exception)
            {
                // We only fail cleanup here, which shouldn't crash the application.
            }
        }
    }
}
