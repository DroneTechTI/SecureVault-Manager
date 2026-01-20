using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecureVault.Core.Interfaces;
using SecureVault.Core.Models;

namespace SecureVault.App.ViewModels;

/// <summary>
/// ViewModel for the Dashboard view showing security overview
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly IVaultService _vaultService;
    private readonly IPasswordAnalysisService _analysisService;

    [ObservableProperty]
    private SecurityScore? _securityScore;

    [ObservableProperty]
    private int _totalCredentials;

    [ObservableProperty]
    private int _weakPasswords;

    [ObservableProperty]
    private int _duplicatePasswords;

    [ObservableProperty]
    private int _compromisedPasswords;

    [ObservableProperty]
    private int _strongPasswords;

    [ObservableProperty]
    private string _scoreLevel = "Unknown";

    [ObservableProperty]
    private string _scoreColor = "#666666";

    [ObservableProperty]
    private bool _isLoading;

    public DashboardViewModel(IVaultService vaultService, IPasswordAnalysisService analysisService)
    {
        _vaultService = vaultService;
        _analysisService = analysisService;
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        if (!_vaultService.IsVaultUnlocked)
            return;

        IsLoading = true;

        try
        {
            var credentials = await _vaultService.GetAllCredentialsAsync();
            TotalCredentials = credentials.Count;

            if (credentials.Count > 0)
            {
                SecurityScore = await _analysisService.CalculateSecurityScoreAsync(credentials);

                WeakPasswords = SecurityScore.WeakPasswords;
                DuplicatePasswords = SecurityScore.DuplicatePasswords;
                CompromisedPasswords = SecurityScore.CompromisedPasswords;
                StrongPasswords = SecurityScore.StrongPasswords;
                ScoreLevel = SecurityScore.GetScoreLevel();
                ScoreColor = SecurityScore.GetScoreColor();
            }
            else
            {
                SecurityScore = new SecurityScore { OverallScore = 0 };
                ScoreLevel = "No Data";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading dashboard: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
