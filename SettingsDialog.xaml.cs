using Microsoft.UI.Xaml.Controls;

namespace Miiverse_PC
{
    /// <summary>The login settings page inside a ContentDialog.</summary>
    public sealed partial class SettingsDialog : ContentDialog
    {
        /// <summary>Initializes the <see cref="SettingsDialog" />.</summary>
        public SettingsDialog()
        {
            InitializeComponent();
            XamlRoot = App.MainWindow!.Content.XamlRoot;
        }
    }
}
