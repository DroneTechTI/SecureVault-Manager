namespace SecureVault.Core.Interfaces;

/// <summary>
/// Service for checking if passwords have been compromised using HIBP API
/// </summary>
public interface ICompromisedPasswordService
{
    /// <summary>
    /// Checks if a password has been compromised using k-anonymity
    /// </summary>
    Task<(bool IsCompromised, int TimesCompromised)> CheckPasswordAsync(string password);
    
    /// <summary>
    /// Checks multiple passwords for compromise
    /// </summary>
    Task<Dictionary<string, (bool IsCompromised, int TimesCompromised)>> CheckPasswordsAsync(List<string> passwords);
    
    /// <summary>
    /// Gets the hash prefix for k-anonymity (first 5 chars of SHA-1)
    /// </summary>
    string GetHashPrefix(string password);
}
