using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecureVault.Core.Interfaces;
using SecureVault.Core.Models;
using Microsoft.UI.Dispatching;

namespace SecureVault.App.ViewModels;

/// <summary>
/// ViewModel for the Dashboard view showing security overview
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    public readonly IVaultService _vaultService;
    private readonly IPasswordAnalysisService _analysisService;
    private DispatcherQueue? _dispatcherQueue;

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
    
    public void SetDispatcherQueue(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
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
                // Calculate quick estimates without HIBP check
                var quickAnalysis = await AnalyzeQuickAsync(credentials);
                
                WeakPasswords = quickAnalysis.WeakCount;
                DuplicatePasswords = quickAnalysis.DuplicateCount;
                StrongPasswords = quickAnalysis.StrongCount;
                CompromisedPasswords = 0; // Will be updated later
                
                // Estimate initial score
                int estimatedScore = 100;
                estimatedScore -= (int)((WeakPasswords / (double)TotalCredentials) * 30);
                estimatedScore -= (int)((DuplicatePasswords / (double)TotalCredentials) * 30);
                
                SecurityScore = new SecurityScore 
                { 
                    OverallScore = Math.Clamp(estimatedScore, 0, 100),
                    TotalCredentials = TotalCredentials,
                    WeakPasswords = WeakPasswords,
                    DuplicatePasswords = DuplicatePasswords,
                    StrongPasswords = StrongPasswords
                };
                
                ScoreLevel = SecurityScore.GetScoreLevel();
                ScoreColor = SecurityScore.GetScoreColor();
                
                // Check compromised passwords in background (slow operation)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var fullScore = await _analysisService.CalculateSecurityScoreAsync(credentials);
                        
                        // Update on UI thread
                        if (_dispatcherQueue != null)
                        {
                            _dispatcherQueue.TryEnqueue(() =>
                            {
                                SecurityScore = fullScore;
                                WeakPasswords = fullScore.WeakPasswords;
                                DuplicatePasswords = fullScore.DuplicatePasswords;
                                CompromisedPasswords = fullScore.CompromisedPasswords;
                                StrongPasswords = fullScore.StrongPasswords;
                                ScoreLevel = fullScore.GetScoreLevel();
                                ScoreColor = fullScore.GetScoreColor();
                            });
                        }
                        else
                        {
                            // Fallback if DispatcherQueue not set
                            SecurityScore = fullScore;
                            WeakPasswords = fullScore.WeakPasswords;
                            DuplicatePasswords = fullScore.DuplicatePasswords;
                            CompromisedPasswords = fullScore.CompromisedPasswords;
                            StrongPasswords = fullScore.StrongPasswords;
                            ScoreLevel = fullScore.GetScoreLevel();
                            ScoreColor = fullScore.GetScoreColor();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in full analysis: {ex.Message}");
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
    
    private async Task<(int WeakCount, int DuplicateCount, int StrongCount)> AnalyzeQuickAsync(List<Credential> credentials)
    {
        return await Task.Run(() =>
        {
            int weakCount = 0;
            int strongCount = 0;
            
            // Quick strength check without HIBP
            foreach (var cred in credentials)
            {
                var strength = _analysisService.CalculatePasswordStrength(cred.Password);
                if (strength.Score < 60)
                    weakCount++;
                else if (strength.Score >= 80)
                    strongCount++;
            }
            
            // Check duplicates
            var duplicates = _analysisService.FindDuplicatePasswords(credentials);
            int duplicateCount = duplicates.SelectMany(g => g.Value).Count();
            
            return (weakCount, duplicateCount, strongCount);
        });
    }
}
