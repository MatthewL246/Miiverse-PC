using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Windows.Storage;
using Windows.System;

namespace Miiverse_PC
{
    /// <summary>The main window of the app.</summary>
    public sealed partial class MainWindow : Window
    {
        /// <summary>The default Pretendo account server (official).</summary>
        private const string defaultAccountServer = "https://account.pretendo.cc";

        /// <summary>The default Pretendo Miiverse discovery server (official).</summary>
        private const string defaultDiscoveryServer = "https://discovery.olv.pretendo.cc";

        /// <summary>The path where account data is stored as JSON.</summary>
        private readonly string accountDataJsonPath;

        /// <summary>The current window's window handle.</summary>
        private readonly IntPtr hwnd;

        /// <summary>The JavaScript code that is run on NavigationCompleted.</summary>
        private readonly string javascriptCode;

        /// <summary>The currently logged-in account.</summary>
        private Account? currentAccount;

        /// <summary>If the WebView is navigating to a page.</summary>
        private bool isWebViewNavigating = false;

        /// <summary>
        ///   Code that runs on initialization of the <see cref="MainWindow" /> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            Title = "Miiverse PC Client";

            javascriptCode = ReadJavascriptFile(@"js\portal.js");

            // Set up combo box bindings
            languageBox.ItemsSource = Enum.GetValues(typeof(LanguageId));
            languageBox.SelectedItem = LanguageId.English;
            countryBox.ItemsSource = Enum.GetValues(typeof(CountryId));
            countryBox.SelectedItem = CountryId.UnitedStates;

            // Set up and restore storage
            try
            {
                accountDataJsonPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "accountData.json");
            }
            catch (InvalidOperationException)
            {
                // The app is running unpackaged, so LocalFolder cannot be used
                // Use the program's folder instead or fall back to the current
                // working directory
                var currentAssembly = System.Reflection.Assembly.GetEntryAssembly();
                string? currentDirectory = Path.GetDirectoryName(currentAssembly?.Location);
                accountDataJsonPath = currentDirectory is not null
                    ? Path.Combine(currentDirectory, "accountData.json")
                    : Path.Combine(Environment.CurrentDirectory, "accountData.json");
            }

            _ = LoadAccountDataAsync();
        }

        /// <summary>
        ///   Normalizes a server name by adding "https://" to the beginning if
        ///   it is not already there.
        /// </summary>
        /// <param name="server">The non-normalized server name.</param>
        /// <returns>The server name after normalization.</returns>
        private static string NormalizeServerName(string server)
        {
            string normalizedServer = server;
            if (!server.StartsWith("http"))
            {
                normalizedServer = "https://" + server;
            }
            return normalizedServer;
        }

        /// <summary>
        ///   Reads JavaScript code from a file path relative to the current
        ///   assembly and handles exceptions by returning a JavaScript alert
        ///   containing the exception text.
        /// </summary>
        /// <param name="filePath">The relative file path of the code.</param>
        /// <returns>
        ///   A string containing the JavaScript code in the file or an alert
        ///   containing exception info.
        /// </returns>
        private static string ReadJavascriptFile(string filePath)
        {
            try
            {
                var currentAssembly = System.Reflection.Assembly.GetEntryAssembly();
                string? currentDirectory = Path.GetDirectoryName(currentAssembly?.Location);
                return currentDirectory is null
                    ? $"alert(\"Failed to get the current application directory.\\n Assembly: {currentAssembly}\\n Directory: {currentDirectory}\")"
                    : File.ReadAllText(Path.Combine(currentDirectory, filePath));
            }
            catch (Exception ex)
            {
                // Basic string replacements that escape new lines, quotes, and
                // backslashes inside the exception text to avoid syntax errors
                string escapedEx = ex.ToString()
                    .Replace("\\", "\\\\")
                    .Replace(Environment.NewLine, "\\n\\n")
                    .Replace("'", "\\'")
                    .Replace("\"", "\\\"");
                return $"alert(\"There was an error loading the JavaScript code at {filePath}: {escapedEx}\"); console.error(\"{escapedEx}\");";
            }
        }

        /// <summary>Clears the stored account data asynchronously.</summary>
        private async Task ClearAccountDataAsync()
        {
            try
            {
                File.Delete(accountDataJsonPath);
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Failed to delete account data", ex.ToString());
            }
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

        /// <summary>Loads the stored account data and settings asynchronously.</summary>
        private async Task LoadAccountDataAsync()
        {
            try
            {
                if (!File.Exists(accountDataJsonPath))
                {
                    // If the file does not exist, there is nothing to load
                    return;
                }

                string jsonData = await File.ReadAllTextAsync(accountDataJsonPath);

                if (string.IsNullOrWhiteSpace(jsonData))
                {
                    // Or if the file is empty
                    return;
                }

                var accountNode = JsonNode.Parse(jsonData)!;

                // Values can safely be null and get converted an empty string
                username.Text = accountNode["username"]?.ToString();
                password.Password = accountNode["passwordHash"]?.ToString();

                accountServer.Text = accountNode["accountServer"]?.ToString();
                discoveryServer.Text = accountNode["discoveryServer"]?.ToString();
                portalServer.Text = accountNode["portalServer"]?.ToString();

                // Values that need to have defaults if null
                languageBox.SelectedItem = (LanguageId?)(int?)accountNode["languageId"] ?? LanguageId.English;
                countryBox.SelectedItem = (CountryId?)(int?)accountNode["countryId"] ?? CountryId.UnitedStates;
                consoleSelect.SelectedItem = (string?)accountNode["console"] ?? "Wii U";

                saveLoginInfo.IsChecked = true;
            }
            catch (Exception ex)
            {
                // Need to wait until the main window is loaded before showing
                // the error due to Content.XamlRoot being null
                while (Content.XamlRoot is null)
                {
                    await Task.Delay(100);
                }
                await ShowErrorDialogAsync
                (
                    "Failed to load the saved account data",
                    "The saved data will be automatically deleted.\n\n" + ex.ToString()
                ).ConfigureAwait(false);
                await ClearAccountDataAsync().ConfigureAwait(false);
            }
        }

        /// <summary>Logs in as a PNID with a username and password hash.</summary>
        private async void LoginAsync(object sender, RoutedEventArgs e)
        {
            // Check if inputs are valid
            if (string.IsNullOrWhiteSpace(username.Text) || string.IsNullOrWhiteSpace(password.Password))
            {
                await ShowErrorDialogAsync
                (
                    "Username or password is empty",
                    "Please fill out both the username and password hash text boxes."
                ).ConfigureAwait(false);
                return;
            }

            // Start login process
            UpdateLoginStatus(true);

            // Process and normalize server names, setting defaults if empty
            if (string.IsNullOrWhiteSpace(accountServer.Text))
            {
                accountServer.Text = defaultAccountServer;
                discoveryServer.Text = defaultDiscoveryServer;
            }
            else
            {
                accountServer.Text = NormalizeServerName(accountServer.Text);

                if (string.IsNullOrWhiteSpace(discoveryServer.Text))
                {
                    discoveryServer.Text = accountServer.Text;
                }

                discoveryServer.Text = NormalizeServerName(discoveryServer.Text);
            }

            // Create the account and account status string
            currentAccount = new(username.Text, password.Password, accountServer.Text, discoveryServer.Text);
            string currentError = "(No error)";
            string currentStatus = "Starting login process";

            try
            {
                var platform = (consoleSelect.SelectedItem as string) == "3DS" ? PlatformId.ThreeDS : PlatformId.WiiU;

                // Hash the password if necessary
                await currentAccount.HashPnidPasswordAsync();
                if (currentAccount.PnidPasswordHash is null)
                {
                    currentError = "Login failed (password hash)";
                    currentStatus = $"The PNID username {currentAccount.PnidUsername} does not exist on the server.";
                    return;
                }

                // Login with password hash and receive OAuth2 token
                currentStatus = await currentAccount.CreateOauth2TokenAsync();
                if (currentAccount.OauthToken is null)
                {
                    currentError = "Login failed (OAuth2 token)";
                    return;
                }

                // Login with OAuth2 token and receive Miiverse service token
                currentStatus = await currentAccount.CreateMiiverseTokenAsync();
                if (currentAccount.MiiverseToken is null)
                {
                    currentError = "Login failed (Miiverse service token)";
                    return;
                }

                // If the Miiverse portal server has been set by the UI, use it.
                // Otherwise, login with the Miiverse token and receive portal server
                if (!string.IsNullOrWhiteSpace(portalServer.Text))
                {
                    portalServer.Text = NormalizeServerName(portalServer.Text);
                    currentAccount.MiiversePortalServer = portalServer.Text;
                }
                else
                {
                    currentStatus = await currentAccount.GetMiiversePortalServerAsync(platform);
                    if (currentAccount.MiiversePortalServer is null)
                    {
                        currentError = "Login failed (Miiverse portal server)";
                        return;
                    }
                }

                // Create the ParamPack data using the selected language and country
                currentAccount.CreateParamPack((LanguageId)languageBox.SelectedItem, (CountryId)countryBox.SelectedItem, platform);

                // At this point, account login has succeeded
                currentStatus = "Successfully logged in.";
            }
            catch (HttpRequestException ex)
            {
                currentError = "Login failed (network error)";
                currentStatus = "The login failed because of a network error: " + ex.ToString();
            }
            catch (System.Xml.XmlException ex)
            {
                currentError = "Login failed (parse error)";
                currentStatus = "The login failed because the account server's response could not be parsed: " + ex.ToString();
            }
            catch (Exception ex)
            {
                currentError = "Login failed (unknown error)";
                currentStatus = "The login failed because of an unknown error: " + ex.ToString();
            }
            finally
            {
                UpdateLoginStatus();

                // Show error dialog if the login failed
                if (!currentAccount.IsSignedIn)
                {
                    await ShowErrorDialogAsync(currentError, currentStatus).ConfigureAwait(false);
                }
            }

            if (!currentAccount.IsSignedIn)
            {
                // Also return if the login failed without an exception
                return;
            }

            // Save or clear the stored login info
            if (saveLoginInfo.IsChecked == true)
            {
                await SaveAccountDataAsync();
            }
            else
            {
                await ClearAccountDataAsync();
            }

            // Set up the WebView and save login info if the login succeeded and
            // no exceptions were thrown
            if (currentAccount.IsSignedIn)
            {
                webView.CoreWebView2.AddWebResourceRequestedFilter($"{currentAccount.MiiversePortalServer}/*", CoreWebView2WebResourceContext.All);
                try
                {
                    // The Miiverse portal server cannot be null due to
                    // Account.IsSignedIn being true
                    webView.Source = new(currentAccount.MiiversePortalServer!);
                }
                catch (UriFormatException ex)
                {
                    await ShowErrorDialogAsync
                    (
                        "Invalid Miiverse portal server",
                        $"The Miiverse portal server (${currentAccount.MiiversePortalServer}) is not a valid URL.\n{ex}"
                    ).ConfigureAwait(false);
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
        private void MiiverseWebResourceRequestedHandler(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs e)
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
                password.Focus(FocusState.Programmatic);
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
                try
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
                catch (UriFormatException ex)
                {
                    _ = ShowErrorDialogAsync
                    (
                        "Invalid Miiverse portal server",
                        $"The Miiverse portal server (${currentAccount.MiiversePortalServer}) is not a valid URL.\n{ex}"
                    );
                }
            }
        }

        /// <summary>Reload or stops loading the WebView.</summary>
        private void ReloadOrStop(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isWebViewNavigating)
                {
                    webView.CoreWebView2.Stop();
                }
                else
                {
                    webView.Reload();
                }
            }
            catch (InvalidOperationException ex)
            {
                // The WebView is not initialized, so the reload failed
                _ = ShowErrorDialogAsync("Error: WebView not initialized", ex.ToString());
            }
        }

        /// <summary>Saves the current account's login info and settings.</summary>
        private async Task SaveAccountDataAsync()
        {
            try
            {
                JsonObject accountDataObject = new()
                {
                    ["username"] = currentAccount?.PnidUsername,
                    ["passwordHash"] = currentAccount?.PnidPasswordHash,

                    ["accountServer"] = accountServer.Text,
                    ["discoveryServer"] = discoveryServer.Text,
                    ["portalServer"] = portalServer.Text,

                    ["languageId"] = (int)languageBox.SelectedItem,
                    ["countryId"] = (int)countryBox.SelectedItem,
                    ["console"] = (string)consoleSelect.SelectedItem,
                };

                JsonSerializerOptions options = new() { WriteIndented = true };

                await File.WriteAllTextAsync(accountDataJsonPath, accountDataObject.ToJsonString(options));
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Failed to save account data", ex.ToString());
            }
        }

        /// <summary>
        ///   Sets up the WebView event handlers asynchronously after it loads.
        /// </summary>
        private async void SetupWebViewHandlersAsync(object sender, RoutedEventArgs e)
        {
            await webView.EnsureCoreWebView2Async();
            webView.NavigationStarting += (sender, e) =>
            {
                reloadButton.Content = "Stop";
                isWebViewNavigating = true;
            };
            webView.NavigationCompleted += (sender, e) =>
            {
                reloadButton.Content = "Reload";
                isWebViewNavigating = false;
            };
            webView.CoreWebView2.HistoryChanged += HistoryChangedHandler;
            webView.CoreWebView2.WebResourceRequested += MiiverseWebResourceRequestedHandler;
            webView.NavigationCompleted += (sender, e) => _ = webView.ExecuteScriptAsync(javascriptCode);
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

        /// <summary>
        ///   Executes JavaScript to click on the hidden image input, opening a
        ///   file explorer window to select am image to upload.
        /// </summary>
        private void UploadImage(object sender, RoutedEventArgs e)
        {
            _ = webView.ExecuteScriptAsync("document.getElementById('hidden-image-file-input').click();");
        }
    }
}
