using SecureVault.Core.Models;

namespace SecureVault.Core.Interfaces;

/// <summary>
/// Service for analyzing password security
/// </summary>
public interface IPasswordAnalysisService
{
    /// <summary>
    /// Analyzes a single credential's password
    /// </summary>
    Task<PasswordAnalysisResult> AnalyzeCredentialAsync(Credential credential);
    
    /// <summary>
    /// Analyzes all credentials in the vault
    /// </summary>
    Task<List<PasswordAnalysisResult>> AnalyzeAllCredentialsAsync(List<Credential> credentials);
    
    /// <summary>
    /// Calculates password strength score (0-100)
    /// </summary>
    PasswordStrengthDetails CalculatePasswordStrength(string password);
    
    /// <summary>
    /// Finds duplicate passwords across credentials
    /// </summary>
    Dictionary<string, List<Credential>> FindDuplicatePasswords(List<Credential> credentials);
    
    /// <summary>
    /// Identifies weak passwords
    /// </summary>
    List<Credential> FindWeakPasswords(List<Credential> credentials, int minimumStrength = 60);
    
    /// <summary>
    /// Checks if password contains common patterns
    /// </summary>
    bool HasCommonPatterns(string password);
    
    /// <summary>
    /// Checks if password is in common dictionary
    /// </summary>
    bool IsInCommonDictionary(string password);
    
    /// <summary>
    /// Calculates overall security score for all credentials
    /// </summary>
    Task<SecurityScore> CalculateSecurityScoreAsync(List<Credential> credentials);
}
