using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecureVault.Core.Interfaces;
using System.IO;

namespace SecureVault.App.ViewModels;

/// <summary>
/// ViewModel for authentication (unlock/create vault)
/// </summary>
public partial class AuthenticationViewModel : ObservableObject
{
    private readonly IVaultService _vaultService;
    private readonly string _vaultPath;

    [ObservableProperty]
    private string _masterPassword = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private bool _isCreatingVault;

    [ObservableProperty]
    private bool _isUnlocking;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public bool VaultExists => File.Exists(_vaultPath);

    public AuthenticationViewModel(IVaultService vaultService)
    {
        _vaultService = vaultService;
        
        // Default vault path
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SecureVault");
        
        if (!Directory.Exists(appDataPath))
            Directory.CreateDirectory(appDataPath);

        _vaultPath = Path.Combine(appDataPath, "vault.db");
    }

    [RelayCommand]
    private async Task UnlockVaultAsync()
    {
        if (string.IsNullOrWhiteSpace(MasterPassword))
        {
            ShowError("⚠️ Inserisci la tua password principale");
            return;
        }

        IsUnlocking = true;
        ClearError();

        try
        {
            var success = await _vaultService.UnlockVaultAsync(MasterPassword);

            if (success)
            {
                // Navigate to main view - handled by the view
                OnVaultUnlocked?.Invoke();
            }
            else
            {
                ShowError("❌ Password errata! Riprova.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Failed to unlock vault: {ex.Message}");
        }
        finally
        {
            IsUnlocking = false;
            MasterPassword = string.Empty;
        }
    }

    [RelayCommand]
    private async Task CreateVaultAsync()
    {
        if (string.IsNullOrWhiteSpace(MasterPassword))
        {
            ShowError("⚠️ Inserisci una password principale");
            return;
        }

        if (MasterPassword.Length < 8)
        {
            ShowError("⚠️ La password deve essere almeno di 8 caratteri");
            return;
        }

        if (MasterPassword != ConfirmPassword)
        {
            ShowError("❌ Le password non corrispondono");
            return;
        }

        IsCreatingVault = true;
        ClearError();

        try
        {
            var success = await _vaultService.InitializeVaultAsync(MasterPassword);

            if (success)
            {
                // Navigate to main view - handled by the view
                OnVaultCreated?.Invoke();
            }
            else
            {
                ShowError("Failed to create vault. Please try again.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Failed to create vault: {ex.Message}");
        }
        finally
        {
            IsCreatingVault = false;
            MasterPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    private void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }

    // Events for view navigation
    public event Action? OnVaultUnlocked;
    public event Action? OnVaultCreated;
}
