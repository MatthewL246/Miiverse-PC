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
    }
}
