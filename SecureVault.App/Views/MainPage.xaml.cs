using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SecureVault.App.ViewModels;

namespace SecureVault.App.Views
{
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; }

        public MainPage()
        {
            this.InitializeComponent();
            
            ViewModel = App.Services.GetRequiredService<MainViewModel>();
            
            // Load initial data
            _ = ViewModel.RefreshDataAsync();
            
            // Navigate to Dashboard by default
            ContentFrame.Navigate(typeof(DashboardPage));
            NavView.SelectedItem = NavView.MenuItems[0];
        }

        private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(SettingsPage));
            }
            else if (args.SelectedItemContainer != null)
            {
                var tag = args.SelectedItemContainer.Tag.ToString();
                
                switch (tag)
                {
                    case "Dashboard":
                        ContentFrame.Navigate(typeof(DashboardPage));
                        break;
                    case "Credentials":
                        ContentFrame.Navigate(typeof(CredentialsPage));
                        break;
                    case "Import":
                        ContentFrame.Navigate(typeof(ImportPage));
                        break;
                    case "Generator":
                        ContentFrame.Navigate(typeof(GeneratorPage));
                        break;
                }
            }
        }

        private async void OnLockVaultClick(object sender, RoutedEventArgs e)
        {
            await ViewModel.LockVaultCommand.ExecuteAsync(null);
            
            // Navigate back to authentication
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
            else
            {
                Frame.Navigate(typeof(AuthenticationPage));
            }
        }
    }
}
