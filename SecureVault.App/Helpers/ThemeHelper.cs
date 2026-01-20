using Microsoft.UI.Xaml;
using Windows.Storage;

namespace SecureVault.App.Helpers;

/// <summary>
/// Helper class for managing app theme
/// </summary>
public static class ThemeHelper
{
    private const string ThemeKey = "AppTheme";

    public static ElementTheme GetCurrentTheme()
    {
        var themeSetting = ApplicationData.Current.LocalSettings.Values[ThemeKey]?.ToString();
        
        return themeSetting switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }

    public static void SetTheme(ElementTheme theme)
    {
        var themeName = theme switch
        {
            ElementTheme.Light => "Light",
            ElementTheme.Dark => "Dark",
            _ => "Default"
        };

        ApplicationData.Current.LocalSettings.Values[ThemeKey] = themeName;

        if (App.MainWindow?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
        }
    }
}
