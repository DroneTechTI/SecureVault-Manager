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
    private readonly IVaultService _vaultService;

    public CredentialsPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<CredentialsViewModel>();
        _vaultService = App.Services.GetRequiredService<IVaultService>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        LoadingRing.IsActive = true;
        
        // Show quick count first
        var quickCount = await _vaultService.GetAllCredentialsAsync();
        CredentialsList.ItemsSource = null;
        
        if (quickCount.Count > 500)
        {
            // For large datasets, show a message
            var warningText = new TextBlock
            {
                Text = $"⏳ Caricamento di {quickCount.Count} credenziali...\n\nL'analisi di sicurezza potrebbe richiedere qualche minuto.\n\nPuoi usare la ricerca per trovare subito una credenziale specifica.",
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(20),
                FontSize = 16
            };
            // Note: We'll load in background
        }
        
        // Load in background to not freeze UI
        _ = Task.Run(async () =>
        {
            await ViewModel.LoadCredentialsCommand.ExecuteAsync(null);
            
            DispatcherQueue.TryEnqueue(() =>
            {
                CredentialsList.ItemsSource = ViewModel.Credentials;
                LoadingRing.IsActive = false;
            });
        });
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
