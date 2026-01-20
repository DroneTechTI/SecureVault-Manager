using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SecureVault.Core.Interfaces;
using Windows.Storage.Pickers;

namespace SecureVault.App.Views;

public sealed partial class ImportPage : Page
{
    private readonly IImportService _importService;
    private readonly IVaultService _vaultService;

    public ImportPage()
    {
        this.InitializeComponent();
        _importService = App.Services.GetRequiredService<IImportService>();
        _vaultService = App.Services.GetRequiredService<IVaultService>();
    }

    private async void OnImportChromeClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.FileTypeFilter.Add(".csv");
            var file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                ResultsText.Text = "⏳ Importazione in corso...\nQuesto potrebbe richiedere alcuni minuti per file grandi.";
                ResultsPanel.Visibility = Visibility.Visible;

                var result = await _importService.ImportFromChromeAsync(file.Path);
                
                if (result.ImportedCredentials.Count > 0)
                {
                    ResultsText.Text = $"⏳ Salvando {result.ImportedCredentials.Count} credenziali...\nAttendi, non chiudere l'app.";
                    
                    // Save in batches for better performance
                    int saved = 0;
                    int batchSize = 100;
                    for (int i = 0; i < result.ImportedCredentials.Count; i += batchSize)
                    {
                        var batch = result.ImportedCredentials.Skip(i).Take(batchSize);
                        foreach (var credential in batch)
                        {
                            await _vaultService.AddCredentialAsync(credential);
                            saved++;
                        }
                        
                        // Update progress
                        ResultsText.Text = $"⏳ Salvate {saved}/{result.ImportedCredentials.Count} credenziali...";
                    }

                    ResultsText.Text = $"✅ Importate con successo {result.SuccessfulImports} credenziali da Chrome!\n\n";
                    ResultsText.Text += "💡 Le credenziali sono state salvate. L'analisi di sicurezza verrà eseguita in background.\n\n";
                    
                    if (result.Errors.Count > 0)
                    {
                        ResultsText.Text += $"⚠️ {result.Errors.Count} avvisi (probabilmente debug info, ignora se l'import è riuscito):\n";
                        ResultsText.Text += string.Join("\n", result.Errors.Where(e => !e.StartsWith("DEBUG")).Take(3));
                    }
                }
                else
                {
                    ResultsText.Text = "❌ Nessuna credenziale trovata nel file.\n\nVerifica che il file CSV sia corretto.";
                    if (result.Errors.Count > 0)
                    {
                        ResultsText.Text += "\n\nDettagli errore:\n" + string.Join("\n", result.Errors.Take(5));
                    }
                }
                
                ResultsPanel.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            ResultsText.Text = $"❌ Errore durante l'importazione:\n{ex.Message}";
            ResultsPanel.Visibility = Visibility.Visible;
        }
    }

    private async void OnImportSamsungClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.FileTypeFilter.Add(".csv");
            picker.FileTypeFilter.Add(".json");
            var file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                ResultsText.Text = "⏳ Importazione in corso...\nQuesto potrebbe richiedere alcuni minuti per file grandi.";
                ResultsPanel.Visibility = Visibility.Visible;

                var result = await _importService.ImportFromSamsungPassAsync(file.Path);
                
                if (result.ImportedCredentials.Count > 0)
                {
                    ResultsText.Text = $"⏳ Salvando {result.ImportedCredentials.Count} credenziali...\nAttendi, non chiudere l'app.";
                    
                    // Save in batches for better performance
                    int saved = 0;
                    int batchSize = 100;
                    for (int i = 0; i < result.ImportedCredentials.Count; i += batchSize)
                    {
                        var batch = result.ImportedCredentials.Skip(i).Take(batchSize);
                        foreach (var credential in batch)
                        {
                            await _vaultService.AddCredentialAsync(credential);
                            saved++;
                        }
                        
                        // Update progress
                        ResultsText.Text = $"⏳ Salvate {saved}/{result.ImportedCredentials.Count} credenziali...";
                    }

                    ResultsText.Text = $"✅ Importate con successo {result.SuccessfulImports} credenziali da Samsung Pass!\n\n";
                    ResultsText.Text += "💡 Le credenziali sono state salvate. L'analisi di sicurezza verrà eseguita in background.\n\n";
                    
                    if (result.Errors.Count > 0)
                    {
                        ResultsText.Text += $"⚠️ {result.Errors.Count} errori:\n" + string.Join("\n", result.Errors.Take(3));
                    }
                }
                else
                {
                    ResultsText.Text = "❌ Nessuna credenziale trovata nel file.\n\nVerifica che il file sia corretto.";
                }
                
                ResultsPanel.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            ResultsText.Text = $"❌ Errore durante l'importazione:\n{ex.Message}";
            ResultsPanel.Visibility = Visibility.Visible;
        }
    }
}
