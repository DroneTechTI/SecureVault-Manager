using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using SecureVault.App.ViewModels;

namespace SecureVault.App.Views;

public sealed partial class CredentialsPage : Page
{
    public CredentialsViewModel ViewModel { get; }

    public CredentialsPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<CredentialsViewModel>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadCredentialsCommand.ExecuteAsync(null);
        
        // Bind credentials to ListView
        CredentialsList.ItemsSource = ViewModel.Credentials;
        LoadingRing.IsActive = ViewModel.IsLoading;
    }

    private async void OnSearchKeyPressed(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        await ViewModel.SearchCommand.ExecuteAsync(null);
    }

    private void OnFilterAll(object sender, RoutedEventArgs e)
    {
        ViewModel.FilterMode = "All";
        ViewModel.ApplyFilterCommand.Execute(null);
    }

    private void OnFilterWeak(object sender, RoutedEventArgs e)
    {
        ViewModel.FilterMode = "Weak";
        ViewModel.ApplyFilterCommand.Execute(null);
    }

    private void OnFilterDuplicate(object sender, RoutedEventArgs e)
    {
        ViewModel.FilterMode = "Duplicate";
        ViewModel.ApplyFilterCommand.Execute(null);
    }

    private void OnFilterCompromised(object sender, RoutedEventArgs e)
    {
        ViewModel.FilterMode = "Compromised";
        ViewModel.ApplyFilterCommand.Execute(null);
    }
}
