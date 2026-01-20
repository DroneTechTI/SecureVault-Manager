using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SecureVault.Core.Interfaces;
using SecureVault.Core.Models;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace SecureVault.App.Views;

public sealed partial class ExportPage : Page
{
    private readonly IExportService _exportService;
    private readonly IVaultService _vaultService;

    public ExportPage()
    {
        this.InitializeComponent();
        _exportService = App.Services.GetRequiredService<IExportService>();
        _vaultService = App.Services.GetRequiredService<IVaultService>();
    }

    private async void OnExportCsvClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var savePicker = new FileSavePicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("CSV File", new[] { ".csv" });
            savePicker.SuggestedFileName = $"SecureVault_Export_{DateTime.Now:yyyyMMdd_HHmmss}";

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                var credentials = await _vaultService.GetAllCredentialsAsync();
                var success = await _exportService.ExportToCsvAsync(credentials, file.Path, IncludePasswordsToggle.IsOn);

                if (success)
                {
                    ResultsText.Text = $"✅ Esportate con successo {credentials.Count} credenziali!\n\nFile salvato: {file.Path}";
                }
                else
                {
                    ResultsText.Text = "❌ Errore durante l'esportazione.";
                }
                ResultsPanel.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            ResultsText.Text = $"❌ Errore: {ex.Message}";
            ResultsPanel.Visibility = Visibility.Visible;
        }
    }

    private async void OnExportJsonClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var savePicker = new FileSavePicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("JSON File", new[] { ".json" });
            savePicker.SuggestedFileName = $"SecureVault_Export_{DateTime.Now:yyyyMMdd_HHmmss}";

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                var credentials = await _vaultService.GetAllCredentialsAsync();
                var encrypt = !string.IsNullOrEmpty(ExportPasswordBox.Password);
                var success = await _exportService.ExportToJsonAsync(credentials, file.Path, encrypt, ExportPasswordBox.Password);

                if (success)
                {
                    var encType = encrypt ? "cifrato" : "non cifrato";
                    ResultsText.Text = $"✅ Esportate con successo {credentials.Count} credenziali in JSON {encType}!\n\nFile salvato: {file.Path}";
                }
                else
                {
                    ResultsText.Text = "❌ Errore durante l'esportazione.";
                }
                ResultsPanel.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            ResultsText.Text = $"❌ Errore: {ex.Message}";
            ResultsPanel.Visibility = Visibility.Visible;
        }
    }

    private async void OnCreateBackupClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var backupFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "SecureVault",
                "Backups");

            Directory.CreateDirectory(backupFolder);
            
            var backupFile = Path.Combine(backupFolder, $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            
            var success = await _exportService.CreateBackupAsync(backupFile);

            if (success)
            {
                ResultsText.Text = $"✅ Backup creato con successo!\n\nPercorso: {backupFile}";
            }
            else
            {
                ResultsText.Text = "❌ Errore durante la creazione del backup.";
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
