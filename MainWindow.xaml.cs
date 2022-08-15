﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Windows.Storage;
using Windows.System;

namespace Miiverse_PC
{
    /// <summary>The main window of the app.</summary>
    public sealed partial class MainWindow : Window
    {
        /// <summary>The current window's window handle.</summary>
        private readonly IntPtr hwnd;

        /// <summary>The currently logged-in account.</summary>
        private Account? currentAccount;

        /// <summary>
        ///   Code that runs on initialization of the <see cref="MainWindow" />
        ///   class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            Title = "Miiverse PC Client";

            // Set up combo box bindings
            languageBox.ItemsSource = Enum.GetValues(typeof(LanguageId));
            languageBox.SelectedItem = LanguageId.English;
            countryBox.ItemsSource = Enum.GetValues(typeof(CountryId));
            countryBox.SelectedItem = CountryId.UnitedStates;
        }

        /// <summary>
        ///   Normalizes a server name by adding "http://" to the beginning if
        ///   it is not already there.
        /// </summary>
        /// <param name="server">The non-normalized server name.</param>
        /// <returns>The server name after normalization.</returns>
        private static string NormalizeServerName(string server)
        {
            string normalizedServer = server;
            if (!server.StartsWith("http"))
            {
                normalizedServer = "http://" + server;
            }
            return normalizedServer;
        }

        /// <summary>
        ///   Downloads the current account's profile data asynchronously and
        ///   prompts the user to save it.
        /// </summary>
        private async void DownloadUserProfileDataAsync(object sender, RoutedEventArgs e)
        {
            if (currentAccount is null || !currentAccount.IsSignedIn)
            {
                await ShowErrorDialogAsync("Not signed in", "You must be signed in to download user profile data.");
                return;
            }

            var savePicker = new Windows.Storage.Pickers.FileSavePicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads,
                SuggestedFileName = "ProfileData"
            };
            savePicker.FileTypeChoices.Add("XML data", new List<string>() { ".xml" });

            // Initialize the file picker with the current window handle. This
            // prevents the file picker from throwing a COM exception
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            StorageFile dataFile = await savePicker.PickSaveFileAsync();

            if (dataFile is not null)
            {
                try
                {
                    string profileData = await currentAccount.GetUserProfileXmlAsync();
                    var profileXml = System.Xml.Linq.XDocument.Parse(profileData);
                    await FileIO.WriteTextAsync(dataFile, profileXml.ToString());
                }
                catch (Exception ex)
                {
                    await ShowErrorDialogAsync("Failed to save profile data", ex.ToString()).ConfigureAwait(false);
                }
            }
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
        ///   Logs in as a PNID with a username and password hash.
        /// </summary>
        private async void LoginAsync(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(username.Text) || string.IsNullOrWhiteSpace(passwordHash.Password))
            {
                await ShowErrorDialogAsync
                (
                    "Username or password is empty",
                    "Please fill out both the username and password hash text boxes."
                ).ConfigureAwait(false);
                return;
            }
            if (passwordHash.Password.Length != 64)
            {
                await ShowErrorDialogAsync
                (
                    "Invalid password hash",
                    "The password hash is not the correct length. Make sure to only copy and paste the 64-character hash."
                ).ConfigureAwait(false);
                return;
            }

            string currentStatus;
            UpdateLoginStatus(true);
            currentAccount = new(username.Text, passwordHash.Password);

            if (!string.IsNullOrWhiteSpace(accountServer.Text))
            {
                string accountServerName = NormalizeServerName(accountServer.Text);
                currentAccount.AccountServer = accountServerName;
                accountServer.Text = accountServerName;

                if (string.IsNullOrWhiteSpace(discoveryServer.Text))
                {
                    discoveryServer.Text = accountServer.Text;
                }

                string discoveryServerName = NormalizeServerName(discoveryServer.Text);
                currentAccount.MiiverseDiscoveryServer = discoveryServerName;
                discoveryServer.Text = discoveryServerName;
            }
            if (!string.IsNullOrWhiteSpace(portalServer.Text))
            {
                string portalServerName = NormalizeServerName(portalServer.Text);
                currentAccount.MiiversePortalServer = portalServerName;

                portalServer.Text = portalServerName;
            }

            try
            {
                var platform = (consoleSelect.SelectedItem as string) == "3DS" ? PlatformId.ThreeDS : PlatformId.WiiU;

                currentStatus = await currentAccount.CreateOauth2TokenAsync();
                if (currentAccount.OauthToken is null)
                {
                    await ShowErrorDialogAsync("Login failed", currentStatus);
                    UpdateLoginStatus();
                    return;
                }
                currentStatus = await currentAccount.CreateMiiverseTokenAsync();
                if (currentAccount.MiiverseToken is null)
                {
                    await ShowErrorDialogAsync("Login failed", currentStatus);
                    UpdateLoginStatus();
                    return;
                }
                if (string.IsNullOrEmpty(currentAccount.MiiversePortalServer))
                {
                    // Only get the Miiverse portal server if it has not already
                    // been set by the UI
                    currentStatus = await currentAccount.GetMiiversePortalServerAsync(platform);
                    if (currentAccount.MiiversePortalServer is null)
                    {
                        await ShowErrorDialogAsync("Login failed", currentStatus);
                        UpdateLoginStatus();
                        return;
                    }
                }
                currentAccount.CreateParamPack((LanguageId)languageBox.SelectedItem, (CountryId)countryBox.SelectedItem, platform);
            }
            catch (HttpRequestException ex)
            {
                await ShowErrorDialogAsync("Login failed (network error)", "The login failed because of a network error: " + ex.ToString());
            }
            catch (System.Xml.XmlException ex)
            {
                await ShowErrorDialogAsync("Login failed (parse error)", "The login failed because the account server's response could not be parsed: " + ex.ToString());
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Login failed (unknown error)", "The login failed because of an unknown error: " + ex.ToString());
            }
            finally
            {
                UpdateLoginStatus();
            }

            if (currentAccount.IsSignedIn)
            {
                webView.CoreWebView2.AddWebResourceRequestedFilter($"{currentAccount.MiiversePortalServer}/*", CoreWebView2WebResourceContext.All);
                try
                {
                    // The Miiverse portal server cannot be null due to
                    // Account.IsSignedIn being true
                    webView.Source = new(currentAccount.MiiversePortalServer!);
                }
                catch (FormatException)
                {
                    await ShowErrorDialogAsync("Invalid Miiverse portal host", $"The Miiverse portal host (${currentAccount.MiiversePortalServer}) is not a valid URL.");
                }
            }
        }

        /// <summary>Logs in when enter is pressed.</summary>
        private void LoginOnEnter(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                // Call LoginAsync without awaiting and use an empty sender
                LoginAsync(new object(), new RoutedEventArgs());
            }
        }

        /// <summary>
        ///   Handles WebResourceRequested events for the Miiverse portal from
        ///   the WebView.
        /// </summary>
        private void MiiverseWebResourceRequestedHandler(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            if (currentAccount is not null && currentAccount.IsSignedIn)
            {
                e.Request.Headers.SetHeader("X-Nintendo-ServiceToken", currentAccount.MiiverseToken);
                e.Request.Headers.SetHeader("X-Nintendo-ParamPack", currentAccount.ParamPackData);
            }
        }

        /// <summary>Moves focus to the to password box on enter.</summary>
        private void MoveToPasswordOnEnter(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                passwordHash.Focus(FocusState.Programmatic);
            }
        }

        /// <summary>
        ///   Navigates the WebView to different Miiverse pages when a button is
        ///   pressed, depending on the button's name.
        /// </summary>
        private void NavigateToPage(object sender, RoutedEventArgs e)
        {
            if (currentAccount is not null && currentAccount.IsSignedIn)
            {
                switch (((Button)sender).Name)
                {
                    case "userPage":
                        webView.Source = new(currentAccount.MiiversePortalServer + "/users/me");
                        break;

                    case "userMenuPage":
                        webView.Source = new(currentAccount.MiiversePortalServer + "/users/menu");
                        break;

                    case "activityFeedPage":
                        webView.Source = new(currentAccount.MiiversePortalServer + "/activity-feed");
                        break;

                    case "communitiesPage":
                        webView.Source = new(currentAccount.MiiversePortalServer + "/communities");
                        break;

                    case "messagesPage":
                        webView.Source = new(currentAccount.MiiversePortalServer + "/messages");
                        break;

                    case "notificationsPage":
                        webView.Source = new(currentAccount.MiiversePortalServer + "/news");
                        break;
                }
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
            webView.CoreWebView2.HistoryChanged += HistoryChangedHandler;
            webView.CoreWebView2.WebResourceRequested += MiiverseWebResourceRequestedHandler;
        }

        /// <summary>Shows an error dialog over the window.</summary>
        /// <param name="title">The error dialog's title.</param>
        /// <param name="message">The error dialog's body.</param>
        /// <returns>A Task object.</returns>
        private async Task ShowErrorDialogAsync(string title, string message)
        {
            ContentDialog errorDialog = new()
            {
                Title = title,
                Content = message,
                CloseButtonText = "Close",
                XamlRoot = Content.XamlRoot
            };
            _ = await errorDialog.ShowAsync().AsTask().ConfigureAwait(false);
        }

        /// <summary>Updates the login status text and button.</summary>
        /// <param name="isLoggingIn">
        ///   Whether an account is currently being logged in to (if true,
        ///   disables the login button).
        /// </param>
        private void UpdateLoginStatus(bool isLoggingIn = false)
        {
            if (isLoggingIn)
            {
                loginArea.Header = "Login status: Logging in...";
                loginButton.IsEnabled = false;
            }
            else
            {
                loginArea.Header = currentAccount is null
                    || !currentAccount.IsSignedIn
                    ? "Login status: Not logged in"
                    : $"Login status: Logged in as \"{currentAccount.PnidUsername}\"";
                loginButton.IsEnabled = true;
            }
        }
    }
}
