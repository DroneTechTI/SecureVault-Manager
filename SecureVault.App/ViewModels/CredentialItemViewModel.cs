using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecureVault.Core.Interfaces;
using SecureVault.Core.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using SecureVault.App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace SecureVault.App.ViewModels;

/// <summary>
/// ViewModel for a single credential item
/// </summary>
public partial class CredentialItemViewModel : ObservableObject
{
    private readonly IVaultService _vaultService;
    private readonly IPasswordGeneratorService _generatorService;
    private readonly Credential _credential;
    private readonly PasswordAnalysisResult? _analysis;
    private readonly LocalizationService _localization;

    public string Id => _credential.Id;
    public string Title => _credential.Title;
    public string Username => _credential.Username;
    public string Email => _credential.Email;
    public string Password => _credential.Password;
    public string Url => _credential.Url;
    public string Domain => _credential.Domain;
    public string Notes => _credential.Notes;
    public bool HasNotes => !string.IsNullOrWhiteSpace(_credential.Notes);
    public string[] Tags => _credential.Tags;
    public bool HasTags => _credential.Tags != null && _credential.Tags.Length > 0;
    public string TagsDisplay => HasTags ? string.Join(", ", _credential.Tags) : "";

    [ObservableProperty]
    private int _passwordStrength;

    [ObservableProperty]
    private bool _isWeak;

    [ObservableProperty]
    private bool _isDuplicate;

    [ObservableProperty]
    private bool _isCompromised;

    [ObservableProperty]
    private string _statusColor = "#4CAF50";
    
    public Windows.UI.Color StatusColorValue
    {
        get
        {
            try
            {
                var hex = StatusColor.TrimStart('#');
                return Windows.UI.Color.FromArgb(
                    255,
                    Convert.ToByte(hex.Substring(0, 2), 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16)
                );
            }
            catch
            {
                return Windows.UI.Color.FromArgb(255, 76, 175, 80); // Default green
            }
        }
    }

    [ObservableProperty]
    private string _statusText = "Strong";

    [ObservableProperty]
    private bool _isPasswordVisible;

    [ObservableProperty]
    private bool _isFavorite;

    public CredentialItemViewModel(
        Credential credential,
        PasswordAnalysisResult? analysis,
        IVaultService vaultService,
        IPasswordGeneratorService generatorService)
    {
        _credential = credential;
        _analysis = analysis;
        _vaultService = vaultService;
        _generatorService = generatorService;
        _localization = App.Services.GetRequiredService<LocalizationService>();
        _isFavorite = credential.IsFavorite;

        if (analysis != null)
        {
            PasswordStrength = analysis.Strength;
            IsWeak = analysis.IsWeak;
            IsDuplicate = analysis.IsDuplicate;
            IsCompromised = analysis.IsCompromised;

            UpdateStatus();
        }
    }

    [RelayCommand]
    private void CopyUsername()
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(Username);
        Clipboard.SetContent(dataPackage);
    }

    [RelayCommand]
    private void CopyPassword()
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(Password);
        Clipboard.SetContent(dataPackage);
    }

    [RelayCommand]
    private async Task OpenUrlAsync()
    {
        if (!string.IsNullOrEmpty(Url))
        {
            await Launcher.LaunchUriAsync(new Uri(Url));
        }
    }

    [RelayCommand]
    private async Task GenerateNewPasswordAsync()
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

        var newPassword = _generatorService.GeneratePassword(options);
        
        _credential.Password = newPassword;
        _credential.LastPasswordChange = DateTime.UtcNow;
        
        await _vaultService.UpdateCredentialAsync(_credential);
        
        OnPropertyChanged(nameof(Password));
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        IsFavorite = !IsFavorite;
        _credential.IsFavorite = IsFavorite;
        
        try
        {
            await _vaultService.UpdateCredentialAsync(_credential);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating favorite status: {ex.Message}");
            // Revert on error
            IsFavorite = !IsFavorite;
            _credential.IsFavorite = IsFavorite;
        }
    }

    private void UpdateStatus()
    {
        if (IsCompromised)
        {
            StatusColor = "#F44336";
            StatusText = _localization.Get("Compromised");
        }
        else if (IsDuplicate)
        {
            StatusColor = "#FF9800";
            StatusText = _localization.Get("Duplicate");
        }
        else if (IsWeak)
        {
            StatusColor = "#FFC107";
            StatusText = _localization.Get("Weak");
        }
        else
        {
            StatusColor = "#4CAF50";
            StatusText = _localization.Get("Strong");
        }
    }
}
