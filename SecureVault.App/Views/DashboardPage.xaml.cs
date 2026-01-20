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
        
        // Show total count immediately
        LoadingRing.IsActive = true;
        
        try
        {
            // Get quick count without analysis
            var credentials = await ViewModel._vaultService.GetAllCredentialsAsync();
            TotalCredentialsText.Text = credentials.Count.ToString();
            ScoreLevelText.Text = "Analisi in corso...";
            ScoreText.Text = "...";
            
            // Start analysis in background without blocking
            _ = Task.Run(async () =>
            {
                await ViewModel.LoadDataCommand.ExecuteAsync(null);
                
                // Update UI on UI thread
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.SecurityScore != null)
                    {
                        ScoreText.Text = ViewModel.SecurityScore.OverallScore.ToString();
                        ScoreLevelText.Text = ViewModel.ScoreLevel;
                        StrongPasswordsText.Text = ViewModel.StrongPasswords.ToString();
                        WeakPasswordsText.Text = ViewModel.WeakPasswords.ToString();
                        CompromisedPasswordsText.Text = ViewModel.CompromisedPasswords.ToString();
                    }
                });
            });
            
            LoadingRing.IsActive = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard error: {ex.Message}");
            LoadingRing.IsActive = false;
        }
    }
}
