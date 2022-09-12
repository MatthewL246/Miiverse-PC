using Microsoft.UI.Xaml.Controls;

namespace Miiverse_PC
{
    /// <summary>The login settings page inside a ContentDialog.</summary>
    internal sealed partial class SettingsDialog : ContentDialog
    {
        /// <summary>Initializes the <see cref="SettingsDialog" />.</summary>
        public SettingsDialog()
        {
            InitializeComponent();

            // Set up combo box bindings
            language.ItemsSource = Enum.GetValues(typeof(LanguageId));
            language.SelectedItem = LanguageId.English;
            country.ItemsSource = Enum.GetValues(typeof(CountryId));
            country.SelectedItem = CountryId.UnitedStates;
        }

        /// <summary>The currently used account settings.</summary>
        public Settings CurrentSettings { get; set; } = new();

        /// <summary>Loads the settings UI from the current settings.</summary>
        public void LoadSettings()
        {
            accountServer.Text = CurrentSettings.AccountServer;
            discoveryServer.Text = CurrentSettings.DiscoveryServer;
            isDiscoveryServerSameAsAccountServer.IsChecked = CurrentSettings.IsDiscoveryServerSameAsAccountServer;
            portalServer.Text = CurrentSettings.PortalServer;
            isDiscoveryServerUsed.IsChecked = CurrentSettings.IsDiscoveryServerUsed;

            language.SelectedItem = CurrentSettings.Language;
            country.SelectedItem = CurrentSettings.Country;
            console.SelectedIndex = CurrentSettings.Platform == PlatformId.ThreeDS ? 1 : 0;

            clientId.Text = CurrentSettings.ConsoleClientId;
            clientSecret.Text = CurrentSettings.ConsoleClientSecret;
            miiverseTitleId.Text = CurrentSettings.MiiverseTitleId;
            allowedServerCertificateHash.Text = CurrentSettings.AllowedServerRootCertificateHash;
        }

        /// <summary>
        ///   Resets the current settings and UI to the default values.
        /// </summary>
        public void ResetSettings()
        {
            CurrentSettings = new();
            LoadSettings();
        }

        /// <summary>Saves the current settings from the settings UI.</summary>
        public void SaveSettings()
        {
            TextBox[] serverTextBoxes = new[] { accountServer, discoveryServer, portalServer };
            foreach (var textBox in serverTextBoxes)
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = NormalizeServerName(textBox.Text);
                }
            }

            if (isDiscoveryServerSameAsAccountServer.IsChecked)
            {
                discoveryServer.Text = accountServer.Text;
            }

            CurrentSettings = new()
            {
                AccountServer = accountServer.Text,
                DiscoveryServer = discoveryServer.Text,
                IsDiscoveryServerSameAsAccountServer = isDiscoveryServerSameAsAccountServer.IsChecked ?? false,
                PortalServer = portalServer.Text,
                IsDiscoveryServerUsed = isDiscoveryServerUsed.IsChecked ?? false,

                Language = (LanguageId)language.SelectedItem,
                Country = (CountryId)country.SelectedItem,
                Platform = console.SelectedIndex == 1 ? PlatformId.ThreeDS : PlatformId.WiiU,

                ConsoleClientId = clientId.Text,
                ConsoleClientSecret = clientSecret.Text,
                MiiverseTitleId = miiverseTitleId.Text,
                AllowedServerRootCertificateHash = allowedServerCertificateHash.Text,
            };
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
        ///   Handles disabling and enabling text boxes when check boxes are checked.
        /// </summary>
        private void SettingsCheckBoxesChecked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            discoveryServer.IsEnabled = !(isDiscoveryServerSameAsAccountServer.IsChecked ?? false) && (isDiscoveryServerUsed.IsChecked ?? false);
            portalServer.IsEnabled = !(isDiscoveryServerUsed.IsChecked ?? false);
        }
    }
}
