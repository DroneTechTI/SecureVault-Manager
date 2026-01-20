namespace SecureVault.Core.Interfaces;

/// <summary>
/// Service for encryption/decryption operations using AES-256-GCM and Argon2id
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Derives a key from the master password using Argon2id
    /// </summary>
    byte[] DeriveKey(string masterPassword, byte[] salt);
    
    /// <summary>
    /// Encrypts data using AES-256-GCM
    /// </summary>
    byte[] Encrypt(byte[] data, byte[] key);
    
    /// <summary>
    /// Decrypts data using AES-256-GCM
    /// </summary>
    byte[] Decrypt(byte[] encryptedData, byte[] key);
    
    /// <summary>
    /// Encrypts a string and returns Base64 encoded result
    /// </summary>
    string EncryptString(string plainText, byte[] key);
    
    /// <summary>
    /// Decrypts a Base64 encoded string
    /// </summary>
    string DecryptString(string encryptedText, byte[] key);
    
    /// <summary>
    /// Generates a cryptographically secure random salt
    /// </summary>
    byte[] GenerateSalt();
    
    /// <summary>
    /// Hashes a password using SHA-256 (for HIBP API)
    /// </summary>
    string HashPassword(string password);
    
    /// <summary>
    /// Securely wipes data from memory
    /// </summary>
    void SecureWipe(byte[] data);
}
