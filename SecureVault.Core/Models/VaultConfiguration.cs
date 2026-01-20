namespace SecureVault.Core.Models;

/// <summary>
/// Configuration for the secure vault
/// </summary>
public class VaultConfiguration
{
    public string VaultPath { get; set; } = string.Empty;
    public int AutoLockMinutes { get; set; } = 5;
    public int ClipboardClearSeconds { get; set; } = 30;
    public bool RequireMasterPasswordOnStartup { get; set; } = true;
    public bool EnableAutoBackup { get; set; } = true;
    public int BackupRetentionDays { get; set; } = 30;
    public string BackupPath { get; set; } = string.Empty;
    public bool CheckForCompromisedPasswords { get; set; } = true;
    public int PasswordStrengthMinimum { get; set; } = 60; // 0-100
}
