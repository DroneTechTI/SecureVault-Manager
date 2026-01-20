using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SecureVault.App.ViewModels;
using SecureVault.Core.Interfaces;

namespace SecureVault.App.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }
    private readonly IVaultService _vaultService;
    private readonly IPasswordAnalysisService _analysisService;

    public DashboardPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<DashboardViewModel>();
        ViewModel.SetDispatcherQueue(DispatcherQueue);
        _vaultService = App.Services.GetRequiredService<IVaultService>();
        _analysisService = App.Services.GetRequiredService<IPasswordAnalysisService>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        LoadingRing.IsActive = true;
        
        try
        {
            System.Diagnostics.Debug.WriteLine("DashboardPage: Starting to load data...");
            
            // Get quick count without analysis
            var credentials = await ViewModel._vaultService.GetAllCredentialsAsync();
            TotalCredentialsText.Text = credentials.Count.ToString();
            ScoreLevelText.Text = "Analisi in corso...";
            ScoreText.Text = "...";
            StrongPasswordsText.Text = "...";
            WeakPasswordsText.Text = "...";
            // CompromisedPasswords now uses binding
            
            LoadingRing.IsActive = false;
            
            System.Diagnostics.Debug.WriteLine($"DashboardPage: Loaded {credentials.Count} credentials, starting analysis...");
            
            // Start analysis in background without blocking
            _ = Task.Run(async () =>
            {
                try
                {
                    await ViewModel.LoadDataCommand.ExecuteAsync(null);
                    
                    System.Diagnostics.Debug.WriteLine("DashboardPage: Analysis complete, updating UI...");
                    
                    // Update UI on UI thread
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (ViewModel.SecurityScore != null)
                        {
                            ScoreText.Text = ViewModel.SecurityScore.OverallScore.ToString();
                            ScoreLevelText.Text = ViewModel.ScoreLevel;
                            StrongPasswordsText.Text = ViewModel.StrongPasswords.ToString();
                            WeakPasswordsText.Text = ViewModel.WeakPasswords.ToString();
                            // CompromisedPasswords now uses binding, no need to set manually
                            
                            System.Diagnostics.Debug.WriteLine($"DashboardPage: UI updated - Compromised: {ViewModel.CompromisedPasswords}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("DashboardPage: SecurityScore is null!");
                        }
                    });
                }
                catch (Exception bgEx)
                {
                    System.Diagnostics.Debug.WriteLine($"DashboardPage Background Error: {bgEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {bgEx.StackTrace}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DashboardPage Critical Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            LoadingRing.IsActive = false;
        }
    }

    private async void OnViewCompromisedClick(object sender, RoutedEventArgs e)
    {
        try
        {
            LoadingRing.IsActive = true;
            
            var credentials = await _vaultService.GetAllCredentialsAsync();
            var results = await _analysisService.AnalyzeAllCredentialsAsync(credentials);
            var compromised = results.Where(r => r.IsCompromised).ToList();

            string message;
            if (compromised.Count == 0)
            {
                message = "✅ Ottimo!\n\nNessuna password compromessa trovata.\n\nTutte le tue password sono sicure secondo il database Have I Been Pwned.";
            }
            else
            {
                message = $"❌ ATTENZIONE!\n\nTrovate {compromised.Count} password compromesse:\n\n";
                
                foreach (var result in compromised.Take(15))
                {
                    var cred = credentials.FirstOrDefault(c => c.Id == result.CredentialId);
                    if (cred != null)
                    {
                        message += $"• {cred.Title}\n";
                        message += $"  {cred.Username}\n";
                        message += $"  Trovata in {result.TimesCompromised:N0} violazioni\n\n";
                    }
                }
                
                if (compromised.Count > 15)
                {
                    message += $"\n... e altre {compromised.Count - 15} password compromesse.\n\n";
                }
                
                message += "⚠️ AZIONE RICHIESTA:\nCambia queste password immediatamente!\n\nVai alla pagina 'Centro Sicurezza' per maggiori dettagli.";
            }

            var dialog = new ContentDialog
            {
                Title = "🔍 Password Compromesse",
                Content = new ScrollViewer 
                { 
                    Content = new TextBlock 
                    { 
                        Text = message,
                        TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap
                    },
                    MaxHeight = 500
                },
                CloseButtonText = "Chiudi",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            var errorDialog = new ContentDialog
            {
                Title = "Errore",
                Content = $"Errore durante la verifica: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();
        }
        finally
        {
            LoadingRing.IsActive = false;
        }
    }
}
