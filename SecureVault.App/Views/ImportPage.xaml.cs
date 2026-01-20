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
        var picker = new FileOpenPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        picker.FileTypeFilter.Add(".csv");
        var file = await picker.PickSingleFileAsync();

        if (file != null)
        {
            var result = await _importService.ImportFromChromeAsync(file.Path);
            
            foreach (var credential in result.ImportedCredentials)
            {
                await _vaultService.AddCredentialAsync(credential);
            }

            ResultsText.Text = $"Successfully imported {result.SuccessfulImports} credentials from Chrome";
            ResultsPanel.Visibility = Visibility.Visible;
        }
    }

    private async void OnImportSamsungClick(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        picker.FileTypeFilter.Add(".csv");
        picker.FileTypeFilter.Add(".json");
        var file = await picker.PickSingleFileAsync();

        if (file != null)
        {
            var result = await _importService.ImportFromSamsungPassAsync(file.Path);
            
            foreach (var credential in result.ImportedCredentials)
            {
                await _vaultService.AddCredentialAsync(credential);
            }

            ResultsText.Text = $"Successfully imported {result.SuccessfulImports} credentials from Samsung Pass";
            ResultsPanel.Visibility = Visibility.Visible;
        }
    }
}
