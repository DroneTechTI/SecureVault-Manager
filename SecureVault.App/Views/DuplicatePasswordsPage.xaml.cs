using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SecureVault.Core.Interfaces;
using SecureVault.Core.Models;
using Windows.System;

namespace SecureVault.App.Views;

public sealed partial class DuplicatePasswordsPage : Page
{
    private readonly IVaultService _vaultService;
    private readonly IPasswordAnalysisService _analysisService;
    private readonly IPasswordGeneratorService _generatorService;
    private Dictionary<string, List<Credential>> _duplicateGroups = new();

    public DuplicatePasswordsPage()
    {
        this.InitializeComponent();
        _vaultService = App.Services.GetRequiredService<IVaultService>();
        _analysisService = App.Services.GetRequiredService<IPasswordAnalysisService>();
        _generatorService = App.Services.GetRequiredService<IPasswordGeneratorService>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await LoadDuplicatesAsync();
    }

    private async Task LoadDuplicatesAsync()
    {
        LoadingRing.IsActive = true;
        DuplicateGroupsPanel.Children.Clear();

        try
        {
            var credentials = await _vaultService.GetAllCredentialsAsync();
            _duplicateGroups = _analysisService.FindDuplicatePasswords(credentials);

            StatsText.Text = $"Trovati {_duplicateGroups.Count} gruppi di password duplicate ({_duplicateGroups.Sum(g => g.Value.Count)} account totali)";

            if (_duplicateGroups.Count == 0)
            {
                var noDataText = new TextBlock
                {
                    Text = "✅ Ottimo! Non ci sono password duplicate.\n\nOgni account ha una password unica.",
                    FontSize = 18,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                DuplicateGroupsPanel.Children.Add(noDataText);
            }
            else
            {
                int groupNum = 1;
                foreach (var (password, accounts) in _duplicateGroups)
                {
                    var groupCard = CreateDuplicateGroupCard(groupNum, accounts);
                    DuplicateGroupsPanel.Children.Add(groupCard);
                    groupNum++;
                }
            }
        }
        catch (Exception ex)
        {
            var errorText = new TextBlock
            {
                Text = $"Errore: {ex.Message}",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red)
            };
            DuplicateGroupsPanel.Children.Add(errorText);
        }
        finally
        {
            LoadingRing.IsActive = false;
        }
    }

    private Border CreateDuplicateGroupCard(int groupNum, List<Credential> accounts)
    {
        var card = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(24),
            BorderThickness = new Thickness(1),
            BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            Margin = new Thickness(0, 0, 0, 16)
        };

        var mainStack = new StackPanel { Spacing = 16 };

        // Header
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var headerText = new TextBlock
        {
            Text = $"Gruppo {groupNum} - {accounts.Count} account con stessa password",
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };

        var actionsStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        actionsStack.SetValue(Grid.ColumnProperty, 1);

        var generateAllBtn = new Button
        {
            Content = "Genera per Tutti",
            Tag = accounts
        };
        generateAllBtn.Click += OnGenerateForGroupClick;

        var openAllBtn = new Button
        {
            Content = "Apri Tutti i Siti",
            Tag = accounts
        };
        openAllBtn.Click += OnOpenGroupSitesClick;

        actionsStack.Children.Add(generateAllBtn);
        actionsStack.Children.Add(openAllBtn);

        headerGrid.Children.Add(headerText);
        headerGrid.Children.Add(actionsStack);

        mainStack.Children.Add(headerGrid);

        // Account list
        foreach (var account in accounts)
        {
            var accountCard = CreateAccountCard(account);
            mainStack.Children.Add(accountCard);
        }

        card.Child = mainStack;
        return card;
    }

    private Border CreateAccountCard(Credential account)
    {
        var card = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 4, 0, 4)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var infoStack = new StackPanel { Spacing = 4 };
        
        var titleText = new TextBlock
        {
            Text = $"🔐 {account.Title}",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        
        var usernameText = new TextBlock
        {
            Text = $"👤 {account.Username}",
            FontSize = 14,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        };

        var urlText = new TextBlock
        {
            Text = $"🌐 {account.Url}",
            FontSize = 12,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        };

        infoStack.Children.Add(titleText);
        infoStack.Children.Add(usernameText);
        if (!string.IsNullOrWhiteSpace(account.Url))
            infoStack.Children.Add(urlText);

        var actionsStack = new StackPanel { Spacing = 8 };
        actionsStack.SetValue(Grid.ColumnProperty, 1);

        var generateBtn = new Button
        {
            Content = "🔐 Genera Nuova",
            Tag = account
        };
        generateBtn.Click += OnGenerateForAccountClick;

        var openSiteBtn = new Button
        {
            Content = "🌐 Apri Sito",
            Tag = account
        };
        openSiteBtn.Click += OnOpenAccountSiteClick;

        var deleteBtn = new Button
        {
            Content = "🗑️ Elimina",
            Tag = account
        };
        deleteBtn.Click += OnDeleteAccountClick;

        var markDoneBtn = new Button
        {
            Content = "✓ Fatto",
            Tag = account,
            Style = (Style)Application.Current.Resources["AccentButtonStyle"]
        };
        markDoneBtn.Click += OnMarkAsDoneClick;

        actionsStack.Children.Add(generateBtn);
        actionsStack.Children.Add(openSiteBtn);
        actionsStack.Children.Add(deleteBtn);
        actionsStack.Children.Add(markDoneBtn);

        grid.Children.Add(infoStack);
        grid.Children.Add(actionsStack);

        card.Child = grid;
        return card;
    }

    private async void OnGenerateForAccountClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var account = button?.Tag as Credential;
        if (account == null) return;

        var options = new PasswordGeneratorOptions
        {
            Length = 16,
            UseUppercase = true,
            UseLowercase = true,
            UseDigits = true,
            UseSpecialChars = true,
            AvoidAmbiguous = true
        };

        var newPassword = _generatorService.GeneratePassword(options);
        account.Password = newPassword;
        account.LastPasswordChange = DateTime.UtcNow;
        account.ModifiedAt = DateTime.UtcNow;

        await _vaultService.UpdateCredentialAsync(account);

        // Show confirmation
        var dialog = new ContentDialog
        {
            Title = "Password Generata",
            Content = $"Nuova password per {account.Title}:\n\n{newPassword}\n\nLa password è stata copiata negli appunti.\nOra apri il sito e cambiale!",
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };

        // Copy to clipboard
        var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
        dataPackage.SetText(newPassword);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

        await dialog.ShowAsync();
    }

    private async void OnOpenAccountSiteClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var account = button?.Tag as Credential;
        if (account == null || string.IsNullOrWhiteSpace(account.Url)) return;

        // Try to open password change page
        var changePasswordUrl = GetPasswordChangeUrl(account.Url);
        await Launcher.LaunchUriAsync(new Uri(changePasswordUrl));
    }

    private async void OnMarkAsDoneClick(object sender, RoutedEventArgs e)
    {
        // Reload to update the list
        await LoadDuplicatesAsync();
    }

    private async void OnGenerateForGroupClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var accounts = button?.Tag as List<Credential>;
        if (accounts == null) return;

        var dialog = new ContentDialog
        {
            Title = "Conferma",
            Content = $"Vuoi generare password uniche per tutti i {accounts.Count} account in questo gruppo?\n\nDopo dovrai cambiarle manualmente su ogni sito.",
            PrimaryButtonText = "Sì, Genera",
            CloseButtonText = "Annulla",
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            foreach (var account in accounts)
            {
                var options = new PasswordGeneratorOptions
                {
                    Length = 16,
                    UseUppercase = true,
                    UseLowercase = true,
                    UseDigits = true,
                    UseSpecialChars = true,
                    AvoidAmbiguous = true
                };

                account.Password = _generatorService.GeneratePassword(options);
                account.LastPasswordChange = DateTime.UtcNow;
                account.ModifiedAt = DateTime.UtcNow;
                await _vaultService.UpdateCredentialAsync(account);
            }

            await LoadDuplicatesAsync();
        }
    }

    private async void OnOpenGroupSitesClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var accounts = button?.Tag as List<Credential>;
        if (accounts == null) return;

        foreach (var account in accounts.Where(a => !string.IsNullOrWhiteSpace(a.Url)))
        {
            var changePasswordUrl = GetPasswordChangeUrl(account.Url);
            await Launcher.LaunchUriAsync(new Uri(changePasswordUrl));
            await Task.Delay(500); // Small delay between opens
        }
    }

    private async void OnGenerateAllClick(object sender, RoutedEventArgs e)
    {
        var totalAccounts = _duplicateGroups.Sum(g => g.Value.Count);
        
        var dialog = new ContentDialog
        {
            Title = "Conferma Generazione Massiva",
            Content = $"Vuoi generare password uniche per TUTTI i {totalAccounts} account con password duplicate?\n\nQuesta operazione sostituirà tutte le password duplicate.",
            PrimaryButtonText = "Sì, Genera Tutto",
            CloseButtonText = "Annulla",
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            LoadingRing.IsActive = true;

            foreach (var (_, accounts) in _duplicateGroups)
            {
                foreach (var account in accounts)
                {
                    var options = new PasswordGeneratorOptions
                    {
                        Length = 16,
                        UseUppercase = true,
                        UseLowercase = true,
                        UseDigits = true,
                        UseSpecialChars = true,
                        AvoidAmbiguous = true
                    };

                    account.Password = _generatorService.GeneratePassword(options);
                    account.LastPasswordChange = DateTime.UtcNow;
                    account.ModifiedAt = DateTime.UtcNow;
                    await _vaultService.UpdateCredentialAsync(account);
                }
            }

            LoadingRing.IsActive = false;
            await LoadDuplicatesAsync();
        }
    }

    private async void OnOpenAllSitesClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Apri Tutti i Siti",
            Content = "Vuoi aprire la pagina di cambio password per tutti gli account con duplicati?\n\nVerranno aperte molte schede del browser!",
            PrimaryButtonText = "Sì, Apri",
            CloseButtonText = "Annulla",
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            foreach (var (_, accounts) in _duplicateGroups)
            {
                foreach (var account in accounts.Where(a => !string.IsNullOrWhiteSpace(a.Url)))
                {
                    var changePasswordUrl = GetPasswordChangeUrl(account.Url);
                    await Launcher.LaunchUriAsync(new Uri(changePasswordUrl));
                    await Task.Delay(500);
                }
            }
        }
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await LoadDuplicatesAsync();
    }

    private async void OnDeleteAccountClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var account = button?.Tag as Credential;
        if (account == null) return;

        var dialog = new ContentDialog
        {
            Title = "Conferma Eliminazione",
            Content = $"Sei sicuro di voler eliminare questo account?\n\n{account.Title}\n{account.Username}\n\n⚠️ Questa operazione NON può essere annullata!",
            PrimaryButtonText = "Elimina",
            CloseButtonText = "Annulla",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await _vaultService.DeleteCredentialAsync(account.Id);
            await LoadDuplicatesAsync();
        }
    }

    private string GetPasswordChangeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "https://google.com";

        try
        {
            var uri = new Uri(url);
            var domain = uri.Host.ToLowerInvariant();

            // Common password change URLs
            if (domain.Contains("google.com"))
                return "https://myaccount.google.com/security";
            if (domain.Contains("facebook.com"))
                return "https://www.facebook.com/settings?tab=security";
            if (domain.Contains("twitter.com") || domain.Contains("x.com"))
                return "https://twitter.com/settings/password";
            if (domain.Contains("amazon.com"))
                return "https://www.amazon.com/ap/cnep";
            if (domain.Contains("microsoft.com"))
                return "https://account.microsoft.com/security";
            if (domain.Contains("apple.com"))
                return "https://appleid.apple.com/account/manage";
            if (domain.Contains("instagram.com"))
                return "https://www.instagram.com/accounts/password/change/";
            if (domain.Contains("linkedin.com"))
                return "https://www.linkedin.com/psettings/change-password";

            // Default: try /settings or /account
            return $"{uri.Scheme}://{uri.Host}/settings";
        }
        catch
        {
            return url;
        }
    }
}
