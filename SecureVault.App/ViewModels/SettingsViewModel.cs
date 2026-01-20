using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecureVault.Core.Interfaces;
using SecureVault.Core.Models;

namespace SecureVault.App.ViewModels;

/// <summary>
/// ViewModel for application settings
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IVaultService _vaultService;
    private readonly IExportService _exportService;
    private VaultConfiguration _configuration = new();

    [ObservableProperty]
    private int _autoLockMinutes;

    [ObservableProperty]
    private int _clipboardClearSeconds;

    [ObservableProperty]
    private bool _enableAutoBackup;

    [ObservableProperty]
    private string _backupPath = string.Empty;

    [ObservableProperty]
    private bool _checkForCompromisedPasswords;

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    [ObservableProperty]
    private string _themeMode = "System"; // Light, Dark, System

    public SettingsViewModel(IVaultService vaultService, IExportService exportService)
    {
        _vaultService = vaultService;
        _exportService = exportService;
        
        LoadSettings();
    }

    private void LoadSettings()
    {
        if (_vaultService.IsVaultUnlocked)
        {
            _configuration = _vaultService.GetConfiguration();
            AutoLockMinutes = _configuration.AutoLockMinutes;
            ClipboardClearSeconds = _configuration.ClipboardClearSeconds;
            EnableAutoBackup = _configuration.EnableAutoBackup;
            BackupPath = _configuration.BackupPath;
            CheckForCompromisedPasswords = _configuration.CheckForCompromisedPasswords;
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (!_vaultService.IsVaultUnlocked)
            return;

        _configuration.AutoLockMinutes = AutoLockMinutes;
        _configuration.ClipboardClearSeconds = ClipboardClearSeconds;
        _configuration.EnableAutoBackup = EnableAutoBackup;
        _configuration.BackupPath = BackupPath;
        _configuration.CheckForCompromisedPasswords = CheckForCompromisedPasswords;

        await _vaultService.UpdateConfigurationAsync(_configuration);
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        if (!_vaultService.IsVaultUnlocked)
            return;

        var backupPath = string.IsNullOrEmpty(BackupPath) 
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SecureVault", "Backups", "backup.json")
            : BackupPath;

        await _exportService.CreateBackupAsync(backupPath);
    }

    [RelayCommand]
    private async Task ChangeMasterPasswordAsync()
    {
        // This will be handled by a dialog in the UI
    }

    [RelayCommand]
    private async Task ExportCredentialsAsync()
    {
        // This will be handled by a dialog in the UI
    }
}
