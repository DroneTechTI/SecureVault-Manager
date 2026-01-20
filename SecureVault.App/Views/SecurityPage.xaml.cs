using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SecureVault.Core.Interfaces;

namespace SecureVault.App.Views;

public sealed partial class SecurityPage : Page
{
    private readonly IVaultService _vaultService;
    private readonly IPasswordAnalysisService _analysisService;

    public SecurityPage()
    {
        this.InitializeComponent();
        _vaultService = App.Services.GetRequiredService<IVaultService>();
        _analysisService = App.Services.GetRequiredService<IPasswordAnalysisService>();
        
        StrengthThresholdSlider.ValueChanged += (s, e) => 
            StrengthValueText.Text = $"Valore: {(int)e.NewValue}";
    }

    private async void OnAnalyzeClick(object sender, RoutedEventArgs e)
    {
        try
        {
            AnalysisProgress.Visibility = Visibility.Visible;
            AnalysisProgress.IsIndeterminate = true;

            var credentials = await _vaultService.GetAllCredentialsAsync();
            var score = await _analysisService.CalculateSecurityScoreAsync(credentials);

            ResultsText.Text = $"📊 ANALISI SICUREZZA COMPLETA\n\n";
            ResultsText.Text += $"Punteggio Sicurezza: {score.OverallScore}/100 ({score.GetScoreLevel()})\n\n";
            ResultsText.Text += $"📈 Statistiche:\n";
            ResultsText.Text += $"• Totale Credenziali: {score.TotalCredentials}\n";
            ResultsText.Text += $"• Password Forti: {score.StrongPasswords} ✅\n";
            ResultsText.Text += $"• Password Deboli: {score.WeakPasswords} ⚠️\n";
            ResultsText.Text += $"• Password Duplicate: {score.DuplicatePasswords} ⚠️\n";
            ResultsText.Text += $"• Password Compromesse: {score.CompromisedPasswords} ❌\n";
            ResultsText.Text += $"• Richiedono Aggiornamento: {score.PasswordsNeedingUpdate}\n";

            ResultsPanel.Visibility = Visibility.Visible;
            AnalysisProgress.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ResultsText.Text = $"❌ Errore durante l'analisi: {ex.Message}";
            ResultsPanel.Visibility = Visibility.Visible;
            AnalysisProgress.Visibility = Visibility.Collapsed;
        }
    }

    private async void OnCheckCompromisedClick(object sender, RoutedEventArgs e)
    {
        try
        {
            AnalysisProgress.Visibility = Visibility.Visible;
            AnalysisProgress.IsIndeterminate = true;

            var credentials = await _vaultService.GetAllCredentialsAsync();
            var results = await _analysisService.AnalyzeAllCredentialsAsync(credentials);
            var compromised = results.Where(r => r.IsCompromised).ToList();

            ResultsText.Text = $"🔍 VERIFICA PASSWORD COMPROMESSE\n\n";
            
            if (compromised.Count == 0)
            {
                ResultsText.Text += "✅ Ottimo! Nessuna password compromessa trovata.\n\n";
                ResultsText.Text += "Tutte le tue password sono sicure secondo il database Have I Been Pwned.";
            }
            else
            {
                ResultsText.Text += $"❌ ATTENZIONE! Trovate {compromised.Count} password compromesse:\n\n";
                
                foreach (var result in compromised.Take(10))
                {
                    var cred = credentials.FirstOrDefault(c => c.Id == result.CredentialId);
                    if (cred != null)
                    {
                        ResultsText.Text += $"• {cred.Title} ({cred.Username})\n";
                        ResultsText.Text += $"  Trovata in {result.TimesCompromised:N0} violazioni\n\n";
                    }
                }
                
                if (compromised.Count > 10)
                {
                    ResultsText.Text += $"\n... e altre {compromised.Count - 10} password compromesse.\n";
                }
                
                ResultsText.Text += "\n⚠️ AZIONE RICHIESTA: Cambia queste password immediatamente!";
            }

            ResultsPanel.Visibility = Visibility.Visible;
            AnalysisProgress.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ResultsText.Text = $"❌ Errore: {ex.Message}";
            ResultsPanel.Visibility = Visibility.Visible;
            AnalysisProgress.Visibility = Visibility.Collapsed;
        }
    }

    private async void OnFindWeakClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var credentials = await _vaultService.GetAllCredentialsAsync();
            var threshold = (int)StrengthThresholdSlider.Value;
            var weak = _analysisService.FindWeakPasswords(credentials, threshold);

            ResultsText.Text = $"🔐 RICERCA PASSWORD DEBOLI (soglia: {threshold})\n\n";
            
            if (weak.Count == 0)
            {
                ResultsText.Text += $"✅ Ottimo! Nessuna password sotto la soglia di {threshold}.\n\n";
                ResultsText.Text += "Tutte le tue password hanno una forza adeguata.";
            }
            else
            {
                ResultsText.Text += $"⚠️ Trovate {weak.Count} password deboli:\n\n";
                
                foreach (var cred in weak.Take(15))
                {
                    var strength = _analysisService.CalculatePasswordStrength(cred.Password);
                    ResultsText.Text += $"• {cred.Title} - Forza: {strength.Score}/100 ({strength.StrengthLevel})\n";
                }
                
                if (weak.Count > 15)
                {
                    ResultsText.Text += $"\n... e altre {weak.Count - 15} password deboli.\n";
                }
                
                ResultsText.Text += "\n💡 Suggerimento: Usa il generatore di password per creare password più forti!";
            }

            ResultsPanel.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ResultsText.Text = $"❌ Errore: {ex.Message}";
            ResultsPanel.Visibility = Visibility.Visible;
        }
    }

    private async void OnFindDuplicatesClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var credentials = await _vaultService.GetAllCredentialsAsync();
            var duplicates = _analysisService.FindDuplicatePasswords(credentials);

            ResultsText.Text = $"🔄 RICERCA PASSWORD DUPLICATE\n\n";
            
            if (duplicates.Count == 0)
            {
                ResultsText.Text += "✅ Eccellente! Ogni account ha una password unica.\n\n";
                ResultsText.Text += "Questa è la best practice consigliata per la sicurezza.";
            }
            else
            {
                ResultsText.Text += $"⚠️ Trovati {duplicates.Count} gruppi di password duplicate:\n\n";
                
                int groupNum = 1;
                foreach (var (password, creds) in duplicates.Take(10))
                {
                    ResultsText.Text += $"Gruppo {groupNum} (usata {creds.Count} volte):\n";
                    foreach (var cred in creds)
                    {
                        ResultsText.Text += $"  • {cred.Title} ({cred.Username})\n";
                    }
                    ResultsText.Text += "\n";
                    groupNum++;
                }
                
                if (duplicates.Count > 10)
                {
                    ResultsText.Text += $"... e altri {duplicates.Count - 10} gruppi.\n\n";
                }
                
                ResultsText.Text += "💡 Suggerimento: Crea una password unica per ogni account!";
            }

            ResultsPanel.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ResultsText.Text = $"❌ Errore: {ex.Message}";
            ResultsPanel.Visibility = Visibility.Visible;
        }
    }
}
