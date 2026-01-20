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
                // Show initial data immediately
                WeakPasswords = 0;
                DuplicatePasswords = 0;
                CompromisedPasswords = 0;
                StrongPasswords = 0;
                ScoreLevel = "Analisi in corso...";
                
                // Calculate in background without blocking
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var score = await _analysisService.CalculateSecurityScoreAsync(credentials);
                        
                        // Update on UI thread
                        SecurityScore = score;
                        WeakPasswords = score.WeakPasswords;
                        DuplicatePasswords = score.DuplicatePasswords;
                        CompromisedPasswords = score.CompromisedPasswords;
                        StrongPasswords = score.StrongPasswords;
                        ScoreLevel = score.GetScoreLevel();
                        ScoreColor = score.GetScoreColor();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error analyzing: {ex.Message}");
                        ScoreLevel = "Errore analisi";
                    }
                });
            }
            else
            {
                SecurityScore = new SecurityScore { OverallScore = 0 };
                ScoreLevel = "Nessun dato";
                WeakPasswords = 0;
                DuplicatePasswords = 0;
                CompromisedPasswords = 0;
                StrongPasswords = 0;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading dashboard: {ex.Message}");
            ScoreLevel = "Errore";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
