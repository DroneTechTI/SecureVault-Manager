using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecureVault.Core.Interfaces;
using SecureVault.Core.Models;
using System.Collections.ObjectModel;

namespace SecureVault.App.ViewModels;

/// <summary>
/// Main ViewModel for the application shell
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IVaultService _vaultService;
    private readonly IPasswordAnalysisService _analysisService;

    [ObservableProperty]
    private bool _isVaultUnlocked;

    [ObservableProperty]
    private string _currentView = "Dashboard";

    [ObservableProperty]
    private SecurityScore? _securityScore;

    [ObservableProperty]
    private int _totalCredentials;

    public DashboardViewModel DashboardViewModel { get; }
    public CredentialsViewModel CredentialsViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }

    public MainViewModel(
        IVaultService vaultService,
        IPasswordAnalysisService analysisService,
        DashboardViewModel dashboardViewModel,
        CredentialsViewModel credentialsViewModel,
        SettingsViewModel settingsViewModel)
    {
        _vaultService = vaultService;
        _analysisService = analysisService;
        
        DashboardViewModel = dashboardViewModel;
        CredentialsViewModel = credentialsViewModel;
        SettingsViewModel = settingsViewModel;

        IsVaultUnlocked = _vaultService.IsVaultUnlocked;
    }

    [RelayCommand]
    private void NavigateTo(string view)
    {
        CurrentView = view;
    }

    [RelayCommand]
    private async Task LockVaultAsync()
    {
        _vaultService.LockVault();
        IsVaultUnlocked = false;
    }

    [RelayCommand]
    private async Task RefreshDataAsync()
    {
        if (!IsVaultUnlocked)
            return;

        var credentials = await _vaultService.GetAllCredentialsAsync();
        TotalCredentials = credentials.Count;
        SecurityScore = await _analysisService.CalculateSecurityScoreAsync(credentials);

        await DashboardViewModel.LoadDataAsync();
        await CredentialsViewModel.LoadCredentialsAsync();
    }
}
