using SecureVault.Core.Models;

namespace SecureVault.Core.Interfaces;

/// <summary>
/// Service for exporting credentials
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exports credentials to a file
    /// </summary>
    Task<bool> ExportAsync(List<Credential> credentials, string filePath, ExportOptions options);
    
    /// <summary>
    /// Exports to CSV format
    /// </summary>
    Task<bool> ExportToCsvAsync(List<Credential> credentials, string filePath, bool includePasswords = true);
    
    /// <summary>
    /// Exports to JSON format
    /// </summary>
    Task<bool> ExportToJsonAsync(List<Credential> credentials, string filePath, bool encrypt = false, string? encryptionPassword = null);
    
    /// <summary>
    /// Creates an encrypted backup of the vault
    /// </summary>
    Task<bool> CreateBackupAsync(string backupPath);
    
    /// <summary>
    /// Restores vault from an encrypted backup
    /// </summary>
    Task<bool> RestoreFromBackupAsync(string backupPath, string masterPassword);
}
