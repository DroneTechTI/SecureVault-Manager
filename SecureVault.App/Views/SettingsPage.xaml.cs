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
        try
        {
            this.InitializeComponent();
            ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
            _localization = App.Services.GetRequiredService<LocalizationService>();
            
            // Set current language after UI is loaded
            this.Loaded += OnPageLoaded;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SettingsPage initialization error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private bool _isLoadingLanguage = false;
    private bool _isLoadingTheme = false;

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _isLoadingLanguage = true;
            
            if (_localization != null && LanguageSelector != null)
            {
                var currentLang = _localization.CurrentLanguage;
                
                // Safely iterate through RadioButtons
                foreach (var item in LanguageSelector.Items)
                {
                    if (item is RadioButton radio && radio.Tag?.ToString() == currentLang)
                    {
                        radio.IsChecked = true;
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnPageLoaded error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        finally
        {
            _isLoadingLanguage = false;
        }
        
        // Load current theme
        try
        {
            _isLoadingTheme = true;
            var currentTheme = App.Current.RequestedTheme;
            string themeTag = currentTheme == ApplicationTheme.Dark ? "Dark" : 
                             currentTheme == ApplicationTheme.Light ? "Light" : "Default";
            
            foreach (var item in ThemeSelector.Items)
            {
                if (item is RadioButton radio && radio.Tag?.ToString() == themeTag)
                {
                    radio.IsChecked = true;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnPageLoaded theme error: {ex.Message}");
        }
        finally
        {
            _isLoadingTheme = false;
        }
    }

    private async void OnLanguageRadioChecked(object sender, RoutedEventArgs e)
    {
        try
        {
            // Prevent triggering during initial load
            if (_isLoadingLanguage) return;
            
            if (sender is RadioButton radio)
            {
                var newLang = radio.Tag?.ToString();
                if (newLang != null && _localization != null && newLang != _localization.CurrentLanguage)
                {
                    _localization.SetLanguage(newLang);
                    
                    // Ensure XamlRoot is available
                    if (this.XamlRoot != null)
                    {
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
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnLanguageRadioChecked error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private async void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (_isLoadingTheme) return;
            
            if (ThemeSelector.SelectedItem is RadioButton radio)
            {
                var themeTag = radio.Tag?.ToString();
                
                ElementTheme newTheme = themeTag switch
                {
                    "Light" => ElementTheme.Light,
                    "Dark" => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
                
                // Apply to main window
                if (App.MainWindow.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = newTheme;
                }
                
                // Save preference
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values["AppTheme"] = themeTag;
                
                var message = _localization.CurrentLanguage == "it" 
                    ? $"Tema cambiato in: {radio.Content}"
                    : $"Theme changed to: {radio.Content}";
                
                var dialog = new ContentDialog
                {
                    Title = _localization.CurrentLanguage == "it" ? "Tema Cambiato" : "Theme Changed",
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                
                await dialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnThemeChanged error: {ex.Message}");
        }
    }
}
