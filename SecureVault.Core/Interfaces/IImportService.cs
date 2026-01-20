using SecureVault.Core.Models;

namespace SecureVault.Core.Interfaces;

/// <summary>
/// Service for importing credentials from external sources
/// </summary>
public interface IImportService
{
    /// <summary>
    /// Imports credentials from a Chrome CSV export
    /// </summary>
    Task<ImportResult> ImportFromChromeAsync(string filePath);
    
    /// <summary>
    /// Imports credentials from Samsung Pass export
    /// </summary>
    Task<ImportResult> ImportFromSamsungPassAsync(string filePath);
    
    /// <summary>
    /// Validates if a file is a valid Chrome export
    /// </summary>
    bool ValidateChromeFile(string filePath);
    
    /// <summary>
    /// Validates if a file is a valid Samsung Pass export
    /// </summary>
    bool ValidateSamsungPassFile(string filePath);
    
    /// <summary>
    /// Gets supported import formats
    /// </summary>
    List<string> GetSupportedFormats();
}
