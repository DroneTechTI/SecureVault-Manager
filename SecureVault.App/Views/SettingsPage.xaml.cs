using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using SecureVault.App.ViewModels;

namespace SecureVault.App.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
    }
}
