using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SecureVault.App.ViewModels;
using SecureVault.App.Services;

namespace SecureVault.App.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }
    private readonly LocalizationService _localization;

    public SettingsPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        _localization = App.Services.GetRequiredService<LocalizationService>();
        
        // Set current language after UI is loaded
        this.Loaded += (s, e) =>
        {
            _isLoadingLanguage = true;
            var currentLang = _localization.CurrentLanguage;
            foreach (var item in LanguageSelector.Items)
            {
                if (item is RadioButton radio && radio.Tag?.ToString() == currentLang)
                {
                    radio.IsChecked = true;
                    break;
                }
            }
            _isLoadingLanguage = false;
        };
    }

    private bool _isLoadingLanguage = false;

    private async void OnLanguageRadioChecked(object sender, RoutedEventArgs e)
    {
        // Prevent triggering during initial load
        if (_isLoadingLanguage) return;
        
        if (sender is RadioButton radio)
        {
            var newLang = radio.Tag?.ToString();
            if (newLang != null && newLang != _localization.CurrentLanguage)
            {
                try
                {
                    _localization.SetLanguage(newLang);
                    
                    var dialog = new ContentDialog
                    {
                        Title = newLang == "it" ? "Lingua Cambiata" : "Language Changed",
                        Content = newLang == "it" ? 
                            "La lingua è stata cambiata in Italiano.\n\nRiavvia l'applicazione per applicare le modifiche." :
                            "Language has been changed to English.\n\nRestart the application to apply changes.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    
                    await dialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Language change error: {ex.Message}");
                }
            }
        }
    }
}
