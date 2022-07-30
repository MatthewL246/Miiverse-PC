using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace Miiverse_PC
{
    /// <summary>The main window of the app.</summary>
    public sealed partial class MainWindow : Window
    {
        /// <summary>
        ///   Code that runs on initialization of the <see cref="MainWindow" />
        ///   class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Title = "Miiverse PC Client";
        }

        /// <summary>Goes back in the WebView.</summary>
        private void GoBack(object sender, RoutedEventArgs e)
        {
            if (webView.CanGoBack)
            {
                webView.GoBack();
            }
        }

        /// <summary>Goes forward in the WebView.</summary>
        private void GoForward(object sender, RoutedEventArgs e)
        {
            if (webView.CanGoForward)
            {
                webView.GoForward();
            }
        }

        /// <summary>Handles HistoryChanged events from the WebView.</summary>
        private void HistoryChangedHandler(CoreWebView2 sender, object args)
        {
            backButton.IsEnabled = sender.CanGoBack;
            forwardButton.IsEnabled = sender.CanGoForward;
            addressBar.Text = sender.Source;
        }

        /// <summary>
        ///   Handles WebResourceRequested events for the Miiverse portal from
        ///   the WebView.
        /// </summary>
        private void MiiverseWebResourceRequestedHandler(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            //e.Request.Headers.SetHeader("Host", "portal.olv.pretendo.cc");
            e.Request.Headers.SetHeader("X-Nintendo-ServiceToken", "ServiceToken");
            e.Request.Headers.SetHeader("X-Nintendo-ParamPack", "ParamPack");
        }

        /// <summary>Navigates the WebView to a URI asynchronously.</summary>
        private async void NavigateToUriAsync(object sender, RoutedEventArgs e)
        {
            string uri = addressBar.Text;
            if (!uri.StartsWith("http"))
            {
                uri = "http://" + uri;
                addressBar.Text = uri;
            }

            try
            {
                webView.Source = new(uri);
            }
            catch (FormatException)
            {
                ContentDialog errorDialog = new()
                {
                    Title = "Invalid URL",
                    Content = "The URL you entered was not valid: " + uri,
                    CloseButtonText = "Close",
                    XamlRoot = Content.XamlRoot
                };
                _ = await errorDialog.ShowAsync();
            }
        }

        /// <summary>Reloads the WebView.</summary>
        private void Reload(object sender, RoutedEventArgs e)
        {
            webView.Reload();
        }

        /// <summary>
        ///   Sets up the WebView event handlers asynchronously after it loads.
        /// </summary>
        private async void SetupWebViewHandlersAsync(object sender, RoutedEventArgs e)
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.AddWebResourceRequestedFilter("*portal.olv.pretendo.cc*", CoreWebView2WebResourceContext.All);
            webView.CoreWebView2.HistoryChanged += HistoryChangedHandler;
            webView.CoreWebView2.WebResourceRequested += MiiverseWebResourceRequestedHandler;
        }
    }
}
