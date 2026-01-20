using System.Text.Json;
using Microsoft.Data.Sqlite;
using SecureVault.Core.Interfaces;
using SecureVault.Core.Models;

namespace SecureVault.Core.Services.Vault;

/// <summary>
/// Professional implementation of encrypted vault using SQLite
/// </summary>
public class VaultService : IVaultService
{
    private readonly IEncryptionService _encryptionService;
    private readonly string _vaultPath;
    private byte[]? _vaultKey;
    private byte[]? _salt;
    private VaultConfiguration _configuration;

    public bool IsVaultUnlocked => _vaultKey != null;

    public VaultService(IEncryptionService encryptionService, string vaultPath)
    {
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _vaultPath = vaultPath ?? throw new ArgumentNullException(nameof(vaultPath));
        _configuration = new VaultConfiguration { VaultPath = vaultPath };
    }

    public async Task<bool> InitializeVaultAsync(string masterPassword)
    {
        if (string.IsNullOrEmpty(masterPassword))
            throw new ArgumentException("Master password cannot be empty", nameof(masterPassword));

        try
        {
            // Generate salt for key derivation
            _salt = _encryptionService.GenerateSalt();
            
            // Derive encryption key from master password
            _vaultKey = _encryptionService.DeriveKey(masterPassword, _salt);

            // Create database
            await CreateDatabaseAsync();
            
            // Store salt (encrypted with a derived key)
            await StoreSaltAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize vault: {ex.Message}");
            LockVault();
            return false;
        }
    }

    public async Task<bool> UnlockVaultAsync(string masterPassword)
    {
        if (string.IsNullOrEmpty(masterPassword))
            throw new ArgumentException("Master password cannot be empty", nameof(masterPassword));

        try
        {
            // Retrieve salt
            _salt = await RetrieveSaltAsync();
            if (_salt == null)
                return false;

            // Derive key from master password
            _vaultKey = _encryptionService.DeriveKey(masterPassword, _salt);

            // Verify key by attempting to read a test credential
            if (!await VerifyKeyAsync())
            {
                LockVault();
                return false;
            }

            // Load configuration
            await LoadConfigurationAsync();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to unlock vault: {ex.Message}");
            LockVault();
            return false;
        }
    }

    public void LockVault()
    {
        if (_vaultKey != null)
        {
            _encryptionService.SecureWipe(_vaultKey);
            _vaultKey = null;
        }
        
        if (_salt != null)
        {
            _encryptionService.SecureWipe(_salt);
            _salt = null;
        }
    }

    public async Task<bool> AddCredentialAsync(Credential credential)
    {
        if (!IsVaultUnlocked)
            throw new InvalidOperationException("Vault is locked");

        if (credential == null)
            throw new ArgumentNullException(nameof(credential));

        try
        {
            credential.ModifiedAt = DateTime.UtcNow;
            
            using var connection = new SqliteConnection($"Data Source={_vaultPath}");
            await connection.OpenAsync();

            var encryptedData = _encryptionService.EncryptString(JsonSerializer.Serialize(credential), _vaultKey!);

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Credentials (Id, EncryptedData, CreatedAt, ModifiedAt, Domain, Title)
                VALUES ($id, $data, $created, $modified, $domain, $title)";
            
            command.Parameters.AddWithValue("$id", credential.Id);
            command.Parameters.AddWithValue("$data", encryptedData);
            command.Parameters.AddWithValue("$created", credential.CreatedAt.ToString("O"));
            command.Parameters.AddWithValue("$modified", credential.ModifiedAt.ToString("O"));
            command.Parameters.AddWithValue("$domain", credential.Domain);
            command.Parameters.AddWithValue("$title", credential.Title);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to add credential: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateCredentialAsync(Credential credential)
    {
        if (!IsVaultUnlocked)
            throw new InvalidOperationException("Vault is locked");

        if (credential == null)
            throw new ArgumentNullException(nameof(credential));

        try
        {
            credential.ModifiedAt = DateTime.UtcNow;
            
            using var connection = new SqliteConnection($"Data Source={_vaultPath}");
            await connection.OpenAsync();

            var encryptedData = _encryptionService.EncryptString(JsonSerializer.Serialize(credential), _vaultKey!);

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Credentials 
                SET EncryptedData = $data, ModifiedAt = $modified, Domain = $domain, Title = $title
                WHERE Id = $id";
            
            command.Parameters.AddWithValue("$id", credential.Id);
            command.Parameters.AddWithValue("$data", encryptedData);
            command.Parameters.AddWithValue("$modified", credential.ModifiedAt.ToString("O"));
            command.Parameters.AddWithValue("$domain", credential.Domain);
            command.Parameters.AddWithValue("$title", credential.Title);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to update credential: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteCredentialAsync(string credentialId)
    {
        if (!IsVaultUnlocked)
            throw new InvalidOperationException("Vault is locked");

        if (string.IsNullOrEmpty(credentialId))
            throw new ArgumentException("Credential ID cannot be empty", nameof(credentialId));

        try
        {
            using var connection = new SqliteConnection($"Data Source={_vaultPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Credentials WHERE Id = $id";
            command.Parameters.AddWithValue("$id", credentialId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to delete credential: {ex.Message}");
            return false;
        }
    }

    public async Task<Credential?> GetCredentialAsync(string credentialId)
    {
        if (!IsVaultUnlocked)
            throw new InvalidOperationException("Vault is locked");

        if (string.IsNullOrEmpty(credentialId))
            throw new ArgumentException("Credential ID cannot be empty", nameof(credentialId));

        try
        {
            using var connection = new SqliteConnection($"Data Source={_vaultPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT EncryptedData FROM Credentials WHERE Id = $id";
            command.Parameters.AddWithValue("$id", credentialId);

            var encryptedData = await command.ExecuteScalarAsync() as string;
            if (string.IsNullOrEmpty(encryptedData))
                return null;

            var json = _encryptionService.DecryptString(encryptedData, _vaultKey!);
            return JsonSerializer.Deserialize<Credential>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get credential: {ex.Message}");
            return null;
        }
    }

    public async Task<List<Credential>> GetAllCredentialsAsync()
    {
        if (!IsVaultUnlocked)
            throw new InvalidOperationException("Vault is locked");

        var credentials = new List<Credential>();

        try
        {
            using var connection = new SqliteConnection($"Data Source={_vaultPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT EncryptedData FROM Credentials ORDER BY ModifiedAt DESC";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var encryptedData = reader.GetString(0);
                var json = _encryptionService.DecryptString(encryptedData, _vaultKey!);
                var credential = JsonSerializer.Deserialize<Credential>(json);
                if (credential != null)
                    credentials.Add(credential);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get all credentials: {ex.Message}");
        }

        return credentials;
    }

    public async Task<List<Credential>> SearchCredentialsAsync(string query)
    {
        if (!IsVaultUnlocked)
            throw new InvalidOperationException("Vault is locked");

        var allCredentials = await GetAllCredentialsAsync();
        
        if (string.IsNullOrWhiteSpace(query))
            return allCredentials;

        var lowerQuery = query.ToLowerInvariant();
        
        return allCredentials.Where(c =>
            c.Title.ToLowerInvariant().Contains(lowerQuery) ||
            c.Username.ToLowerInvariant().Contains(lowerQuery) ||
            c.Email.ToLowerInvariant().Contains(lowerQuery) ||
            c.Domain.ToLowerInvariant().Contains(lowerQuery) ||
            c.Url.ToLowerInvariant().Contains(lowerQuery) ||
            c.Notes.ToLowerInvariant().Contains(lowerQuery)
        ).ToList();
    }

    public async Task<bool> ChangeMasterPasswordAsync(string currentPassword, string newPassword)
    {
        if (!IsVaultUnlocked)
            throw new InvalidOperationException("Vault is locked");

        if (string.IsNullOrEmpty(newPassword))
            throw new ArgumentException("New password cannot be empty", nameof(newPassword));

        try
        {
            // Get all credentials with current key
            var credentials = await GetAllCredentialsAsync();

            // Generate new salt and key
            var newSalt = _encryptionService.GenerateSalt();
            var newKey = _encryptionService.DeriveKey(newPassword, newSalt);

            // Re-encrypt all credentials with new key
            using var connection = new SqliteConnection($"Data Source={_vaultPath}");
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            
            try
            {
                foreach (var credential in credentials)
                {
                    var encryptedData = _encryptionService.EncryptString(JsonSerializer.Serialize(credential), newKey);

                    var command = connection.CreateCommand();
                    command.CommandText = "UPDATE Credentials SET EncryptedData = $data WHERE Id = $id";
                    command.Parameters.AddWithValue("$data", encryptedData);
                    command.Parameters.AddWithValue("$id", credential.Id);
                    await command.ExecuteNonQueryAsync();
                }

                // Update salt
                _encryptionService.SecureWipe(_salt!);
                _salt = newSalt;
                await StoreSaltAsync();

                transaction.Commit();

                // Update current key
                _encryptionService.SecureWipe(_vaultKey!);
                _vaultKey = newKey;

                return true;
            }
            catch
            {
                transaction.Rollback();
                _encryptionService.SecureWipe(newKey);
                _encryptionService.SecureWipe(newSalt);
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to change master password: {ex.Message}");
            return false;
        }
    }

    public VaultConfiguration GetConfiguration()
    {
        return _configuration;
    }

    public async Task<bool> UpdateConfigurationAsync(VaultConfiguration config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        try
        {
            _configuration = config;
            
            // Save to database
            if (IsVaultUnlocked)
            {
                using var connection = new SqliteConnection($"Data Source={_vaultPath}");
                await connection.OpenAsync();

                var configJson = JsonSerializer.Serialize(_configuration);
                var encryptedConfig = _encryptionService.EncryptString(configJson, _vaultKey!);

                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT OR REPLACE INTO Configuration (Key, Value) 
                    VALUES ('config', $value)";
                command.Parameters.AddWithValue("$value", encryptedConfig);
                await command.ExecuteNonQueryAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to update configuration: {ex.Message}");
            return false;
        }
    }

    // Private helper methods

    private async Task CreateDatabaseAsync()
    {
        using var connection = new SqliteConnection($"Data Source={_vaultPath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Credentials (
                Id TEXT PRIMARY KEY,
                EncryptedData TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                ModifiedAt TEXT NOT NULL,
                Domain TEXT,
                Title TEXT
            );

            CREATE INDEX IF NOT EXISTS idx_credentials_domain ON Credentials(Domain);
            CREATE INDEX IF NOT EXISTS idx_credentials_modified ON Credentials(ModifiedAt);

            CREATE TABLE IF NOT EXISTS Metadata (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Configuration (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );
        ";

        await command.ExecuteNonQueryAsync();
    }

    private async Task StoreSaltAsync()
    {
        using var connection = new SqliteConnection($"Data Source={_vaultPath}");
        await connection.OpenAsync();

        var saltBase64 = Convert.ToBase64String(_salt!);

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO Metadata (Key, Value) 
            VALUES ('salt', $salt)";
        command.Parameters.AddWithValue("$salt", saltBase64);

        await command.ExecuteNonQueryAsync();
    }

    private async Task<byte[]?> RetrieveSaltAsync()
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={_vaultPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM Metadata WHERE Key = 'salt'";

            var saltBase64 = await command.ExecuteScalarAsync() as string;
            if (string.IsNullOrEmpty(saltBase64))
                return null;

            return Convert.FromBase64String(saltBase64);
        }
        catch
        {
            return null;
        }
    }

    private async Task<bool> VerifyKeyAsync()
    {
        try
        {
            // Try to read one credential to verify the key is correct
            using var connection = new SqliteConnection($"Data Source={_vaultPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT EncryptedData FROM Credentials LIMIT 1";

            var encryptedData = await command.ExecuteScalarAsync() as string;
            
            // If no credentials exist yet, key is valid
            if (string.IsNullOrEmpty(encryptedData))
                return true;

            // Try to decrypt
            _encryptionService.DecryptString(encryptedData, _vaultKey!);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task LoadConfigurationAsync()
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={_vaultPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM Configuration WHERE Key = 'config'";

            var encryptedConfig = await command.ExecuteScalarAsync() as string;
            if (!string.IsNullOrEmpty(encryptedConfig))
            {
                var configJson = _encryptionService.DecryptString(encryptedConfig, _vaultKey!);
                var config = JsonSerializer.Deserialize<VaultConfiguration>(configJson);
                if (config != null)
                    _configuration = config;
            }
        }
        catch
        {
            // Use default configuration if load fails
        }
    }
}
