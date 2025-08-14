using System.Windows;
// <-- needed for ApartmentState

namespace Aerotest
{


    [SetUpFixture] // Tip: put this class in the *global namespace* to apply to the whole test assembly
    public sealed class WpfBootstrap
    {
        private static bool _initialized;

        [OneTimeSetUp, Apartment(ApartmentState.STA)]
        public void Init()
        {
            if (_initialized) return;

            if (Application.Current == null)
            {
                // Create your real App (this sets Application.Current)
                var app = new Aerochat.App();
                app.InitializeComponent(); // merges App.xaml dictionaries
            }
            else if (Application.Current is Aerochat.App existingApp)
            {
                // Ensure resources are merged if not yet
                if (Application.Current.TryFindResource("Window") is null)
                    existingApp.InitializeComponent();
            }
            else
            {
                // Another Application already exists; we can't create Aerochat.App now.
                // If needed, manually merge the SAME dictionaries as in App.xaml (order matters).
                // Example:
                // var rd = Application.Current.Resources.MergedDictionaries;
                // rd.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/Aerochat;component/Resources/WindowStyles.xaml") });
                // rd.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/Aerochat;component/Resources/Theme.xaml") });
            }

            // Sanity check so failures are obvious
            if (Application.Current.TryFindResource("Window") is null)
                throw new InvalidOperationException(
                    "Bootstrap failed: resource 'Window' not found. Ensure App.xaml dictionaries are merged (and in the correct order).");

            _initialized = true;
        }
    }
}