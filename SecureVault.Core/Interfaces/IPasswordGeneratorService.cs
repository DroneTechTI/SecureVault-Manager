using SecureVault.Core.Models;

namespace SecureVault.Core.Interfaces;

/// <summary>
/// Service for generating secure passwords and passphrases
/// </summary>
public interface IPasswordGeneratorService
{
    /// <summary>
    /// Generates a random password based on options
    /// </summary>
    string GeneratePassword(PasswordGeneratorOptions options);
    
    /// <summary>
    /// Generates a passphrase with random words
    /// </summary>
    string GeneratePassphrase(PasswordGeneratorOptions options);
    
    /// <summary>
    /// Generates multiple password suggestions
    /// </summary>
    List<string> GeneratePasswordSuggestions(PasswordGeneratorOptions options, int count = 5);
    
    /// <summary>
    /// Validates if generated password meets requirements
    /// </summary>
    bool ValidatePassword(string password, PasswordGeneratorOptions options);
}
