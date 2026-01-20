using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using SecureVault.Core.Interfaces;

namespace SecureVault.Core.Services.Encryption;

/// <summary>
/// Professional implementation of encryption using AES-256-GCM and Argon2id
/// </summary>
public class EncryptionService : IEncryptionService
{
    private const int SaltSize = 32; // 256 bits
    private const int KeySize = 32; // 256 bits for AES-256
    private const int NonceSize = 12; // 96 bits for GCM
    private const int TagSize = 16; // 128 bits authentication tag
    
    // Argon2id parameters (OWASP recommendations)
    private const int Argon2Iterations = 3;
    private const int Argon2MemorySize = 65536; // 64 MB
    private const int Argon2Parallelism = 4;

    public byte[] DeriveKey(string masterPassword, byte[] salt)
    {
        if (string.IsNullOrEmpty(masterPassword))
            throw new ArgumentException("Master password cannot be empty", nameof(masterPassword));
        
        if (salt == null || salt.Length != SaltSize)
            throw new ArgumentException($"Salt must be {SaltSize} bytes", nameof(salt));

        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(masterPassword))
        {
            Salt = salt,
            DegreeOfParallelism = Argon2Parallelism,
            MemorySize = Argon2MemorySize,
            Iterations = Argon2Iterations
        };

        return argon2.GetBytes(KeySize);
    }

    public byte[] Encrypt(byte[] data, byte[] key)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));
        
        if (key == null || key.Length != KeySize)
            throw new ArgumentException($"Key must be {KeySize} bytes", nameof(key));

        // Generate random nonce
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        // Create cipher
        using var aesGcm = new AesGcm(key, TagSize);
        
        // Prepare output buffer: nonce + ciphertext + tag
        var ciphertext = new byte[data.Length];
        var tag = new byte[TagSize];
        
        // Encrypt
        aesGcm.Encrypt(nonce, data, ciphertext, tag);
        
        // Combine: nonce + ciphertext + tag
        var result = new byte[NonceSize + ciphertext.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, NonceSize + ciphertext.Length, TagSize);
        
        return result;
    }

    public byte[] Decrypt(byte[] encryptedData, byte[] key)
    {
        if (encryptedData == null || encryptedData.Length < NonceSize + TagSize)
            throw new ArgumentException("Invalid encrypted data", nameof(encryptedData));
        
        if (key == null || key.Length != KeySize)
            throw new ArgumentException($"Key must be {KeySize} bytes", nameof(key));

        // Extract components
        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var ciphertext = new byte[encryptedData.Length - NonceSize - TagSize];
        
        Buffer.BlockCopy(encryptedData, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(encryptedData, NonceSize, ciphertext, 0, ciphertext.Length);
        Buffer.BlockCopy(encryptedData, NonceSize + ciphertext.Length, tag, 0, TagSize);
        
        // Decrypt
        using var aesGcm = new AesGcm(key, TagSize);
        var plaintext = new byte[ciphertext.Length];
        
        try
        {
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
            return plaintext;
        }
        catch (CryptographicException)
        {
            throw new CryptographicException("Decryption failed. Invalid key or corrupted data.");
        }
    }

    public string EncryptString(string plainText, byte[] key)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;
        
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = Encrypt(plainBytes, key);
        return Convert.ToBase64String(encryptedBytes);
    }

    public string DecryptString(string encryptedText, byte[] key)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;
        
        var encryptedBytes = Convert.FromBase64String(encryptedText);
        var plainBytes = Decrypt(encryptedBytes, key);
        return Encoding.UTF8.GetString(plainBytes);
    }

    public byte[] GenerateSalt()
    {
        var salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));
        
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(password));
        return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
    }

    public void SecureWipe(byte[] data)
    {
        if (data != null && data.Length > 0)
        {
            RandomNumberGenerator.Fill(data);
            Array.Clear(data, 0, data.Length);
        }
    }
}
