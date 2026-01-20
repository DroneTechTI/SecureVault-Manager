using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using SecureVault.App.ViewModels;
using SecureVault.Core.Interfaces;
using System.Linq;

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
        CredentialsList.ItemsSource = null;
        
        try
        {
            // Load credentials in batches for better performance
            var credentials = await _vaultService.GetAllCredentialsAsync();
            
            DispatcherQueue.TryEnqueue(() =>
            {
                ViewModel.Credentials.Clear();
                
                // Show first 50 immediately (smaller batch for faster initial load)
                var firstBatch = credentials.Take(50).ToList();
                foreach (var cred in firstBatch)
                {
                    var vm = new CredentialItemViewModel(cred, null, _vaultService, 
                        App.Services.GetRequiredService<IPasswordGeneratorService>());
                    ViewModel.Credentials.Add(vm);
                }
                
                CredentialsList.ItemsSource = ViewModel.Credentials;
                LoadingRing.IsActive = false;
                
                // Load remaining in background with larger batches
                if (credentials.Count > 50)
                {
                    _ = Task.Run(() =>
                    {
                        var remaining = credentials.Skip(50).ToList();
                        var batchSize = 100;
                        
                        for (int i = 0; i < remaining.Count; i += batchSize)
                        {
                            var batch = remaining.Skip(i).Take(batchSize).ToList();
                            
                            DispatcherQueue.TryEnqueue(() =>
                            {
                                foreach (var cred in batch)
                                {
                                    var vm = new CredentialItemViewModel(cred, null, _vaultService,
                                        App.Services.GetRequiredService<IPasswordGeneratorService>());
                                    ViewModel.Credentials.Add(vm);
                                }
                            });
                            
                            // Small delay between batches to keep UI responsive
                            if (i + batchSize < remaining.Count)
                                Task.Delay(5).Wait();
                        }
                    });
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading credentials: {ex.Message}");
            LoadingRing.IsActive = false;
        }
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

    private async void OnGeneratePasswordClick(object sender, RoutedEventArgs e)
    {
        var item = sender as MenuFlyoutItem;
        var vm = item?.Tag as CredentialItemViewModel;
        if (vm != null)
        {
            await vm.GenerateNewPasswordCommand.ExecuteAsync(null);
            
            var dialog = new ContentDialog
            {
                Title = "Password Generata",
                Content = $"Nuova password generata per {vm.Title}!\n\nLa password è stata salvata. Ricordati di cambiarla anche sul sito!",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    private void OnCopyUsernameClick(object sender, RoutedEventArgs e)
    {
        var item = sender as MenuFlyoutItem;
        var vm = item?.Tag as CredentialItemViewModel;
        vm?.CopyUsernameCommand.Execute(null);
    }

    private void OnCopyPasswordClick(object sender, RoutedEventArgs e)
    {
        var item = sender as MenuFlyoutItem;
        var vm = item?.Tag as CredentialItemViewModel;
        vm?.CopyPasswordCommand.Execute(null);
    }

    private async void OnOpenUrlClick(object sender, RoutedEventArgs e)
    {
        var item = sender as MenuFlyoutItem;
        var vm = item?.Tag as CredentialItemViewModel;
        if (vm != null)
        {
            await vm.OpenUrlCommand.ExecuteAsync(null);
        }
    }

    private async void OnDeleteCredentialClick(object sender, RoutedEventArgs e)
    {
        var item = sender as MenuFlyoutItem;
        var vm = item?.Tag as CredentialItemViewModel;
        if (vm == null) return;

        var dialog = new ContentDialog
        {
            Title = "Conferma Eliminazione",
            Content = $"Sei sicuro di voler eliminare questa credenziale?\n\n{vm.Title}\n{vm.Username}\n\n⚠️ Questa operazione NON può essere annullata!",
            PrimaryButtonText = "Elimina",
            CloseButtonText = "Annulla",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteCredentialCommand.ExecuteAsync(vm);
            
            // Refresh list
            ViewModel.Credentials.Remove(vm);
        }
    }

    private void OnTogglePasswordVisibility(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var vm = button?.Tag as CredentialItemViewModel;
        if (vm != null)
        {
            vm.TogglePasswordVisibilityCommand.Execute(null);
        }
    }
}
