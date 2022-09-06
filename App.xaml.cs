﻿using Microsoft.UI.Xaml;

namespace Miiverse_PC
{
    /// <summary>
    ///   Provides application-specific behavior to supplement the default
    ///   Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        ///   Initializes the singleton application object. This is the first
        ///   line of authored code executed, and as such is the logical
        ///   equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>A reference to the current main window.</summary>
        internal static MainWindow? MainWindow { get; private set; }

        /// <summary>
        ///   Invoked when the application is launched normally by the end user.
        ///   Other entry points will be used such as when the application is
        ///   launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();
        }
    }
}
