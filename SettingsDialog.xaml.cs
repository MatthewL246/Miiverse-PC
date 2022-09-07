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
            XamlRoot = App.MainWindow!.Content.XamlRoot;

            // Set up combo box bindings
            language.ItemsSource = Enum.GetValues(typeof(LanguageId));
            language.SelectedItem = LanguageId.English;
            country.ItemsSource = Enum.GetValues(typeof(CountryId));
            country.SelectedItem = CountryId.UnitedStates;
        }

        /// <summary>The currently used account settings.</summary>
        public Settings CurrentSettings { get; set; } = new();

        public void LoadSettings()
        {
            accountServer.Text = CurrentSettings.AccountServer;
            discoveryServer.Text = CurrentSettings.DiscoveryServer;
            portalServer.Text = CurrentSettings.PortalServer;

            language.SelectedItem = CurrentSettings.Language;
            country.SelectedItem = CurrentSettings.Country;
            console.SelectedIndex = CurrentSettings.Platform == PlatformId.ThreeDS ? 1 : 0;

            clientId.Text = CurrentSettings.ConsoleClientId;
            clientSecret.Text = CurrentSettings.ConsoleClientSecret;
            miiverseTitleId.Text = CurrentSettings.MiiverseTitleId;
            allowedServerCertificateHash.Text = CurrentSettings.AllowedServerRootCertificateHash;
        }

        public void ResetSettings()
        {
            CurrentSettings = new();
            LoadSettings();
        }

        public void SaveSettings()
        {
            CurrentSettings = new()
            {
                AccountServer = accountServer.Text,
                DiscoveryServer = discoveryServer.Text,
                PortalServer = portalServer.Text,

                Language = (LanguageId)language.SelectedItem,
                Country = (CountryId)country.SelectedItem,
                Platform = console.SelectedIndex == 1 ? PlatformId.ThreeDS : PlatformId.WiiU,

                ConsoleClientId = clientId.Text,
                ConsoleClientSecret = clientSecret.Text,
                MiiverseTitleId = miiverseTitleId.Text,
                AllowedServerRootCertificateHash = allowedServerCertificateHash.Text,
            };
        }
    }
}
