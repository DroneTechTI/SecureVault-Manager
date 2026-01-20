using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecureVault.Core.Interfaces;
using SecureVault.Core.Models;
using System.Collections.ObjectModel;

namespace SecureVault.App.ViewModels;

/// <summary>
/// ViewModel for managing credentials list
/// </summary>
public partial class CredentialsViewModel : ObservableObject
{
    private readonly IVaultService _vaultService;
    private readonly IPasswordAnalysisService _analysisService;
    private readonly IPasswordGeneratorService _generatorService;

    [ObservableProperty]
    private ObservableCollection<CredentialItemViewModel> _credentials = new();

    [ObservableProperty]
    private CredentialItemViewModel? _selectedCredential;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _filterMode = "All"; // All, Weak, Duplicate, Compromised

    [ObservableProperty]
    private bool _isGroupedView = false;

    [ObservableProperty]
    private ObservableCollection<CredentialGroup> _groupedCredentials = new();

    public CredentialsViewModel(
        IVaultService vaultService,
        IPasswordAnalysisService analysisService,
        IPasswordGeneratorService generatorService)
    {
        _vaultService = vaultService;
        _analysisService = analysisService;
        _generatorService = generatorService;
    }

    [RelayCommand]
    public async Task LoadCredentialsAsync()
    {
        if (!_vaultService.IsVaultUnlocked)
            return;

        IsLoading = true;

        try
        {
            var credentials = await _vaultService.GetAllCredentialsAsync();
            var analysisResults = await _analysisService.AnalyzeAllCredentialsAsync(credentials);

            Credentials.Clear();

            foreach (var credential in credentials)
            {
                var analysis = analysisResults.FirstOrDefault(a => a.CredentialId == credential.Id);
                var viewModel = new CredentialItemViewModel(credential, analysis, _vaultService, _generatorService);
                Credentials.Add(viewModel);
            }

            ApplyFilter();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading credentials: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (!_vaultService.IsVaultUnlocked)
            return;

        IsLoading = true;

        try
        {
            var results = await _vaultService.SearchCredentialsAsync(SearchQuery);
            var analysisResults = await _analysisService.AnalyzeAllCredentialsAsync(results);

            Credentials.Clear();

            foreach (var credential in results)
            {
                var analysis = analysisResults.FirstOrDefault(a => a.CredentialId == credential.Id);
                var viewModel = new CredentialItemViewModel(credential, analysis, _vaultService, _generatorService);
                Credentials.Add(viewModel);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ApplyFilter()
    {
        var allCredentials = Credentials.ToList();

        Credentials.Clear();

        var filtered = FilterMode switch
        {
            "Weak" => allCredentials.Where(c => c.IsWeak),
            "Duplicate" => allCredentials.Where(c => c.IsDuplicate),
            "Compromised" => allCredentials.Where(c => c.IsCompromised),
            _ => allCredentials
        };

        foreach (var credential in filtered)
        {
            Credentials.Add(credential);
        }
    }

    [RelayCommand]
    private async Task DeleteCredentialAsync(CredentialItemViewModel credential)
    {
        if (credential == null)
            return;

        await _vaultService.DeleteCredentialAsync(credential.Id);
        Credentials.Remove(credential);
    }

    [RelayCommand]
    private void ToggleGroupView()
    {
        IsGroupedView = !IsGroupedView;
        
        if (IsGroupedView)
        {
            CreateGroups();
        }
    }

    private void CreateGroups()
    {
        GroupedCredentials.Clear();

        // Group by domain
        var groups = Credentials
            .GroupBy(c => string.IsNullOrEmpty(c.Domain) ? "Altri" : c.Domain)
            .OrderBy(g => g.Key);

        foreach (var group in groups)
        {
            var credGroup = new CredentialGroup
            {
                Domain = group.Key,
                Credentials = new ObservableCollection<CredentialItemViewModel>(group)
            };
            GroupedCredentials.Add(credGroup);
        }
    }
}

public class CredentialGroup
{
    public string Domain { get; set; } = string.Empty;
    public ObservableCollection<CredentialItemViewModel> Credentials { get; set; } = new();
    public int Count => Credentials.Count;
}
