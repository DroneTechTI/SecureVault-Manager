using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SecureVault.App.ViewModels;

namespace SecureVault.App.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<DashboardViewModel>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadDataCommand.ExecuteAsync(null);
        
        // Update UI
        if (ViewModel.SecurityScore != null)
        {
            ScoreText.Text = ViewModel.SecurityScore.OverallScore.ToString();
            ScoreLevelText.Text = ViewModel.ScoreLevel;
            TotalCredentialsText.Text = ViewModel.TotalCredentials.ToString();
            StrongPasswordsText.Text = ViewModel.StrongPasswords.ToString();
            WeakPasswordsText.Text = ViewModel.WeakPasswords.ToString();
            CompromisedPasswordsText.Text = ViewModel.CompromisedPasswords.ToString();
        }
        
        LoadingRing.IsActive = ViewModel.IsLoading;
    }
}
