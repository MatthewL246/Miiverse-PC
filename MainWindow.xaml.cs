using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
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
        /// <summary>The default options for JSON serialization.</summary>
        private static readonly JsonSerializerOptions defaultSerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = {
                new JsonStringEnumConverter()
            },
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };

        /// <summary>The path where account data is stored as JSON.</summary>
        private readonly string accountDataJsonPath;

        /// <summary>The current window's window handle.</summary>
        private readonly IntPtr hwnd;

        /// <summary>The JavaScript code that is run on NavigationCompleted.</summary>
        private readonly string javascriptCode;

        /// <summary>The path where settings data is stored as JSON.</summary>
        private readonly string settingsDataJsonPath;

        /// <summary>
        ///   The settings dialog used to display and edit account settings.
        /// </summary>
        private readonly SettingsDialog settingsDialog = new();

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
            settingsDialog.LoadSettings();

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
            settingsDataJsonPath = Path.Combine(Path.GetDirectoryName(accountDataJsonPath)!, "settings.json");

            // Load account and settings data
            _ = LoadAccountDataAsync();
            _ = LoadSettingsDataAsync();
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

        /// <summary>Clears the stored account data.</summary>
        private async Task DeleteAccountDataAsync()
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

        /// <summary>Clears the stored settings data.</summary>
        private async Task DeleteSettingsDataAsync()
        {
            settingsDialog.ResetSettings();
            try
            {
                File.Delete(settingsDataJsonPath);
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Failed to delete settings data", ex.ToString());
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

        /// <summary>Loads the stored account data asynchronously.</summary>
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

                username.Text = accountNode["username"]?.ToString();
                password.Password = accountNode["passwordHash"]?.ToString();

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
                await DeleteAccountDataAsync().ConfigureAwait(false);
            }
        }

        /// <summary>Loads the stored settings data asynchronously.</summary>
        private async Task LoadSettingsDataAsync()
        {
            try
            {
                if (!File.Exists(settingsDataJsonPath))
                {
                    // If the file does not exist, there is nothing to load
                    return;
                }

                string jsonData = await File.ReadAllTextAsync(settingsDataJsonPath);

                if (string.IsNullOrWhiteSpace(jsonData))
                {
                    // Or if the file is empty
                    return;
                }

                Settings? storedSettings = JsonSerializer.Deserialize<Settings>(jsonData, defaultSerializerOptions);

                // Use default settings if deserialization returns null
                storedSettings ??= new();
                settingsDialog.CurrentSettings = storedSettings;
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
                    "Failed to load the saved settings",
                    "The settings will be automatically reset.\n\n" + ex.ToString()
                ).ConfigureAwait(false);
                await DeleteSettingsDataAsync().ConfigureAwait(false);
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

            // Create the account and account status string
            currentAccount = new(username.Text, password.Password, settingsDialog.CurrentSettings);
            string currentError = "(No error)";
            string currentStatus = "Starting login process";

            try
            {
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
                if (currentAccount.OauthTokenIsNull)
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

                // Login with the Miiverse token and receive portal server
                currentStatus = await currentAccount.GetMiiversePortalServerAsync();
                if (currentAccount.MiiversePortalServer == "https://")
                {
                    currentError = "Login failed (Miiverse portal server)";
                    return;
                }

                // Create the ParamPack data using the selected language and country
                currentAccount.CreateParamPack();

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
                await DeleteAccountDataAsync();
            }

            // Set up the WebView and save login info if the login succeeded and
            // no exceptions were thrown
            if (currentAccount.IsSignedIn)
            {
                webView.CoreWebView2.AddWebResourceRequestedFilter($"{currentAccount.MiiversePortalServer}/*", CoreWebView2WebResourceContext.All);
                try
                {
                    webView.Source = new(currentAccount.MiiversePortalServer);
                }
                catch (UriFormatException ex)
                {
                    await ShowErrorDialogAsync
                    (
                        "Invalid Miiverse portal server",
                        $"The Miiverse portal server ({currentAccount.MiiversePortalServer}) is not a valid URL.\n{ex}"
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
                string page = ((Button)sender).Name switch
                {
                    "userPage" => "/users/me",
                    "userMenuPage" => "/users/menu",
                    "activityFeedPage" => "/activity-feed",
                    "communitiesPage" => "/communities",
                    "messagesPage" => "/messages",
                    "notificationsPage" => "/news",
                    _ => "",
                };
                page = currentAccount.MiiversePortalServer + page;

                try
                {
                    webView.Source = new(page);
                }
                catch (UriFormatException ex)
                {
                    _ = ShowErrorDialogAsync
                    (
                        "Invalid Miiverse portal server",
                        $"The Miiverse portal server (${page}) is not a valid URL.\n{ex}"
                    );
                }
            }
        }

        /// <summary>Opens the settings dialog.</summary>
        private async void OpenSettingsDialogAsync(object sender, RoutedEventArgs e)
        {
            settingsDialog.XamlRoot ??= Content.XamlRoot;
            var result = await settingsDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Save button clicked
                settingsDialog.SaveSettings();

                // Save the settings data as JSON
                await SaveSettingsDataAsync();
            }
            else if (result == ContentDialogResult.Secondary)
            {
                // Reset button clicked
                ContentDialog confirmationDialog = new()
                {
                    Title = "Reset confirmation",
                    Content = "Are you sure that you want to reset the settings?",
                    PrimaryButtonText = "Reset",
                    CloseButtonText = "Cancel",
                    XamlRoot = Content.XamlRoot,
                };
                var confirmation = await confirmationDialog.ShowAsync();

                if (confirmation == ContentDialogResult.Primary)
                {
                    settingsDialog.ResetSettings();

                    // Delete the stored settings data
                    await DeleteSettingsDataAsync();
                }
            }
            else
            {
                // Dialog was closed, do not save settings
                settingsDialog.LoadSettings();
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

        /// <summary>Saves the current account's login info as JSON.</summary>
        private async Task SaveAccountDataAsync()
        {
            try
            {
                JsonObject accountDataObject = new()
                {
                    ["username"] = currentAccount?.PnidUsername,
                    ["passwordHash"] = currentAccount?.PnidPasswordHash,
                };

                string jsonData = accountDataObject.ToJsonString(defaultSerializerOptions);
                await File.WriteAllTextAsync(accountDataJsonPath, jsonData);
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Failed to save account data", ex.ToString());
            }
        }

        /// <summary>Saves the current settings as JSON.</summary>
        private async Task SaveSettingsDataAsync()
        {
            try
            {
                string settingsData = JsonSerializer.Serialize(settingsDialog.CurrentSettings, defaultSerializerOptions);
                await File.WriteAllTextAsync(settingsDataJsonPath, settingsData);
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Failed to save new settings", ex.ToString());
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
                loginStatus.Text = "Logging in...";
                loginButton.IsEnabled = false;
            }
            else
            {
                loginStatus.Text = currentAccount is null
                    || !currentAccount.IsSignedIn
                    ? "Not logged in"
                    : $"Logged in as \"{currentAccount.PnidUsername}\"";
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
