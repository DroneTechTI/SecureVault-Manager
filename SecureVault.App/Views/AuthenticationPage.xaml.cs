using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SecureVault.App.ViewModels;

namespace SecureVault.App.Views;

public sealed partial class AuthenticationPage : Page
{
    public AuthenticationViewModel ViewModel { get; }

    public AuthenticationPage()
    {
        this.InitializeComponent();
        
        ViewModel = App.Services.GetRequiredService<AuthenticationViewModel>();
        ViewModel.OnVaultUnlocked += NavigateToMainPage;
        ViewModel.OnVaultCreated += NavigateToMainPage;

        // Check if vault exists
        if (ViewModel.VaultExists)
        {
            TitleText.Text = "Sblocca Vault";
            UnlockPanel.Visibility = Visibility.Visible;
            CreatePanel.Visibility = Visibility.Collapsed;
            ConfirmPasswordBox.Visibility = Visibility.Collapsed;
        }
        else
        {
            TitleText.Text = "Crea Password Principale";
            UnlockPanel.Visibility = Visibility.Collapsed;
            CreatePanel.Visibility = Visibility.Visible;
            ConfirmPasswordBox.Visibility = Visibility.Visible;
        }
    }

    private void OnPasswordKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            if (UnlockPanel.Visibility == Visibility.Visible)
            {
                OnUnlockClick(null, null);
            }
        }
    }

    private async void OnUnlockClick(object sender, RoutedEventArgs e)
    {
        ViewModel.MasterPassword = MasterPasswordBox.Password;
        await ViewModel.UnlockVaultCommand.ExecuteAsync(null);
    }

    private async void OnCreateClick(object sender, RoutedEventArgs e)
    {
        ViewModel.MasterPassword = MasterPasswordBox.Password;
        ViewModel.ConfirmPassword = ConfirmPasswordBox.Password;
        await ViewModel.CreateVaultCommand.ExecuteAsync(null);
    }

    private void OnCreateVaultClick(object sender, RoutedEventArgs e)
    {
        TitleText.Text = "Crea Password Principale";
        UnlockPanel.Visibility = Visibility.Collapsed;
        CreatePanel.Visibility = Visibility.Visible;
        ConfirmPasswordBox.Visibility = Visibility.Visible;
    }

    private void OnBackToUnlockClick(object sender, RoutedEventArgs e)
    {
        TitleText.Text = "Sblocca Vault";
        UnlockPanel.Visibility = Visibility.Visible;
        CreatePanel.Visibility = Visibility.Collapsed;
        ConfirmPasswordBox.Visibility = Visibility.Collapsed;
    }

    private void NavigateToMainPage()
    {
        Frame.Navigate(typeof(MainPage));
    }
}
