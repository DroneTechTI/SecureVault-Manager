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
    private List<CredentialItemViewModel> _allCredentials = new();

    public CredentialsPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<CredentialsViewModel>();
        _vaultService = App.Services.GetRequiredService<IVaultService>();
        
        // Add search functionality
        SearchBox.TextChanged += OnSearchTextChanged;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        try
        {
            LoadingRing.IsActive = true;
            CredentialsList.ItemsSource = null;
            
            // Clear previous data
            ViewModel.Credentials.Clear();
            _allCredentials.Clear();
            
            System.Diagnostics.Debug.WriteLine("CredentialsPage: Loading credentials...");
            
            // Load credentials in batches for better performance
            var credentials = await _vaultService.GetAllCredentialsAsync();
            
            System.Diagnostics.Debug.WriteLine($"CredentialsPage: Loaded {credentials.Count} credentials");
            
            DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    // Show first 50 immediately (smaller batch for faster initial load)
                    var firstBatch = credentials.Take(50).ToList();
                    System.Diagnostics.Debug.WriteLine($"CredentialsPage: Adding first batch of {firstBatch.Count} items");
                    
                    foreach (var cred in firstBatch)
                    {
                        var vm = new CredentialItemViewModel(cred, null, _vaultService, 
                            App.Services.GetRequiredService<IPasswordGeneratorService>());
                        ViewModel.Credentials.Add(vm);
                        _allCredentials.Add(vm);
                    }
                    
                    CredentialsList.ItemsSource = ViewModel.Credentials;
                    LoadingRing.IsActive = false;
                    
                    System.Diagnostics.Debug.WriteLine("CredentialsPage: First batch loaded successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"CredentialsPage UI Error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    LoadingRing.IsActive = false;
                }
                
                    // Load remaining in background with larger batches
                    if (credentials.Count > 50)
                    {
                        _ = Task.Run(() =>
                        {
                            try
                            {
                                var remaining = credentials.Skip(50).ToList();
                                var batchSize = 100;
                                
                                System.Diagnostics.Debug.WriteLine($"CredentialsPage: Loading remaining {remaining.Count} items in background");
                                
                                for (int i = 0; i < remaining.Count; i += batchSize)
                                {
                                    var batch = remaining.Skip(i).Take(batchSize).ToList();
                                    
                                    DispatcherQueue.TryEnqueue(() =>
                                    {
                                        try
                                        {
                                            foreach (var cred in batch)
                                            {
                                                var vm = new CredentialItemViewModel(cred, null, _vaultService,
                                                    App.Services.GetRequiredService<IPasswordGeneratorService>());
                                                ViewModel.Credentials.Add(vm);
                                                _allCredentials.Add(vm);
                                            }
                                        }
                                        catch (Exception batchEx)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"CredentialsPage Batch Error: {batchEx.Message}");
                                        }
                                    });
                                    
                                    // Small delay between batches to keep UI responsive
                                    if (i + batchSize < remaining.Count)
                                        Task.Delay(5).Wait();
                                }
                                
                                System.Diagnostics.Debug.WriteLine("CredentialsPage: All credentials loaded");
                            }
                            catch (Exception bgEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"CredentialsPage Background Error: {bgEx.Message}");
                            }
                        });
                    }
                });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CredentialsPage Critical Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            
            DispatcherQueue.TryEnqueue(() =>
            {
                LoadingRing.IsActive = false;
            });
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

    private void OnToggleGroupView(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleGroupViewCommand.Execute(null);
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = SearchBox.Text.ToLower().Trim();
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            // Show all credentials
            ViewModel.Credentials.Clear();
            foreach (var cred in _allCredentials)
            {
                ViewModel.Credentials.Add(cred);
            }
        }
        else
        {
            // Filter credentials
            var filtered = _allCredentials.Where(c =>
                c.Title.ToLower().Contains(searchText) ||
                c.Username.ToLower().Contains(searchText) ||
                (!string.IsNullOrEmpty(c.Domain) && c.Domain.ToLower().Contains(searchText))
            ).ToList();
            
            ViewModel.Credentials.Clear();
            foreach (var cred in filtered)
            {
                ViewModel.Credentials.Add(cred);
            }
        }
        
        // Update grouped view if active
        if (ViewModel.IsGroupedView)
        {
            ViewModel.CreateGroups();
        }
    }

    private void OnToggleFavorite(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var vm = button?.Tag as CredentialItemViewModel;
        if (vm != null)
        {
            _ = vm.ToggleFavoriteCommand.ExecuteAsync(null);
        }
    }

    private void OnFilterFavorites(object sender, RoutedEventArgs e)
    {
        ViewModel.Credentials.Clear();
        foreach (var cred in _allCredentials.Where(c => c.IsFavorite))
        {
            ViewModel.Credentials.Add(cred);
        }
        
        if (ViewModel.IsGroupedView)
        {
            ViewModel.CreateGroups();
        }
    }
}
