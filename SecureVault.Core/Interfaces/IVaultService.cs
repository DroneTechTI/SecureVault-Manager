using SecureVault.Core.Models;

namespace SecureVault.Core.Interfaces;

/// <summary>
/// Service for managing the encrypted credential vault
/// </summary>
public interface IVaultService
{
    /// <summary>
    /// Initializes a new vault with a master password
    /// </summary>
    Task<bool> InitializeVaultAsync(string masterPassword);
    
    /// <summary>
    /// Unlocks the vault with the master password
    /// </summary>
    Task<bool> UnlockVaultAsync(string masterPassword);
    
    /// <summary>
    /// Locks the vault and clears sensitive data from memory
    /// </summary>
    void LockVault();
    
    /// <summary>
    /// Checks if the vault is currently unlocked
    /// </summary>
    bool IsVaultUnlocked { get; }
    
    /// <summary>
    /// Adds a new credential to the vault
    /// </summary>
    Task<bool> AddCredentialAsync(Credential credential);
    
    /// <summary>
    /// Updates an existing credential
    /// </summary>
    Task<bool> UpdateCredentialAsync(Credential credential);
    
    /// <summary>
    /// Deletes a credential from the vault
    /// </summary>
    Task<bool> DeleteCredentialAsync(string credentialId);
    
    /// <summary>
    /// Retrieves a credential by ID
    /// </summary>
    Task<Credential?> GetCredentialAsync(string credentialId);
    
    /// <summary>
    /// Retrieves all credentials
    /// </summary>
    Task<List<Credential>> GetAllCredentialsAsync();
    
    /// <summary>
    /// Searches credentials by query
    /// </summary>
    Task<List<Credential>> SearchCredentialsAsync(string query);
    
    /// <summary>
    /// Changes the master password
    /// </summary>
    Task<bool> ChangeMasterPasswordAsync(string currentPassword, string newPassword);
    
    /// <summary>
    /// Gets the vault configuration
    /// </summary>
    VaultConfiguration GetConfiguration();
    
    /// <summary>
    /// Updates the vault configuration
    /// </summary>
    Task<bool> UpdateConfigurationAsync(VaultConfiguration config);
}
