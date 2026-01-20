using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using SecureVault.Core.Interfaces;
using SecureVault.Core.Models;

namespace SecureVault.Core.Services.Export;

/// <summary>
/// Professional export service with support for CSV, JSON, and encrypted backups
/// </summary>
public class ExportService : IExportService
{
    private readonly IEncryptionService _encryptionService;
    private readonly IVaultService _vaultService;

    public ExportService(IEncryptionService encryptionService, IVaultService vaultService)
    {
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _vaultService = vaultService ?? throw new ArgumentNullException(nameof(vaultService));
    }

    public async Task<bool> ExportAsync(List<Credential> credentials, string filePath, ExportOptions options)
    {
        try
        {
            // Filter credentials based on options
            var filteredCredentials = FilterCredentials(credentials, options);

            if (filteredCredentials.Count == 0)
            {
                Console.WriteLine("No credentials to export after filtering");
                return false;
            }

            return options.Format switch
            {
                ExportFormat.Csv => await ExportToCsvAsync(filteredCredentials, filePath, options.IncludePasswords),
                ExportFormat.Json => await ExportToJsonAsync(filteredCredentials, filePath, false, null),
                ExportFormat.EncryptedJson => await ExportToJsonAsync(filteredCredentials, filePath, 
                    options.EncryptExport, options.EncryptionPassword),
                _ => throw new ArgumentException("Unsupported export format")
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Export failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ExportToCsvAsync(List<Credential> credentials, string filePath, bool includePasswords = true)
    {
        try
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            };

            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
            using var csv = new CsvWriter(writer, config);

            // Write header
            csv.WriteField("Title");
            csv.WriteField("URL");
            csv.WriteField("Domain");
            csv.WriteField("Username");
            csv.WriteField("Email");
            
            if (includePasswords)
                csv.WriteField("Password");
            
            csv.WriteField("Notes");
            csv.WriteField("Tags");
            csv.WriteField("Created");
            csv.WriteField("Modified");
            csv.NextRecord();

            // Write data
            foreach (var credential in credentials)
            {
                csv.WriteField(credential.Title);
                csv.WriteField(credential.Url);
                csv.WriteField(credential.Domain);
                csv.WriteField(credential.Username);
                csv.WriteField(credential.Email);
                
                if (includePasswords)
                    csv.WriteField(credential.Password);
                
                csv.WriteField(credential.Notes);
                csv.WriteField(string.Join(", ", credential.Tags));
                csv.WriteField(credential.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                csv.WriteField(credential.ModifiedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                csv.NextRecord();
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CSV export failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ExportToJsonAsync(List<Credential> credentials, string filePath, 
        bool encrypt = false, string? encryptionPassword = null)
    {
        try
        {
            var exportData = new
            {
                ExportedAt = DateTime.UtcNow,
                Version = "1.0",
                Source = "SecureVault Manager",
                TotalCredentials = credentials.Count,
                Credentials = credentials.Select(c => new
                {
                    c.Title,
                    c.Url,
                    c.Domain,
                    c.Username,
                    c.Email,
                    c.Password,
                    c.Notes,
                    c.Tags,
                    CreatedAt = c.CreatedAt.ToString("O"),
                    ModifiedAt = c.ModifiedAt.ToString("O")
                })
            };

            var json = JsonConvert.SerializeObject(exportData, Formatting.Indented);

            if (encrypt && !string.IsNullOrEmpty(encryptionPassword))
            {
                // Generate salt and derive key
                var salt = _encryptionService.GenerateSalt();
                var key = _encryptionService.DeriveKey(encryptionPassword, salt);

                // Encrypt the JSON
                var encryptedData = _encryptionService.EncryptString(json, key);

                // Create encrypted export format
                var encryptedExport = new
                {
                    Format = "SecureVault Encrypted Export",
                    Version = "1.0",
                    Salt = Convert.ToBase64String(salt),
                    Data = encryptedData
                };

                json = JsonConvert.SerializeObject(encryptedExport, Formatting.Indented);

                // Clean up sensitive data
                _encryptionService.SecureWipe(key);
                _encryptionService.SecureWipe(salt);
            }

            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"JSON export failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CreateBackupAsync(string backupPath)
    {
        try
        {
            if (!_vaultService.IsVaultUnlocked)
            {
                Console.WriteLine("Vault must be unlocked to create backup");
                return false;
            }

            // Get all credentials
            var credentials = await _vaultService.GetAllCredentialsAsync();
            var configuration = _vaultService.GetConfiguration();

            // Create backup data
            var backupData = new
            {
                BackupVersion = "1.0",
                CreatedAt = DateTime.UtcNow,
                TotalCredentials = credentials.Count,
                Configuration = configuration,
                Credentials = credentials
            };

            var json = JsonConvert.SerializeObject(backupData, Formatting.Indented);

            // Ensure backup directory exists
            var backupDir = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(backupDir) && !Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            // Generate unique filename with timestamp
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = Path.GetFileNameWithoutExtension(backupPath);
            var extension = Path.GetExtension(backupPath);
            var finalPath = Path.Combine(backupDir ?? ".", $"{fileName}_{timestamp}{extension}");

            await File.WriteAllTextAsync(finalPath, json, Encoding.UTF8);

            Console.WriteLine($"Backup created: {finalPath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Backup creation failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RestoreFromBackupAsync(string backupPath, string masterPassword)
    {
        try
        {
            if (!File.Exists(backupPath))
            {
                Console.WriteLine("Backup file not found");
                return false;
            }

            // Read backup file
            var json = await File.ReadAllTextAsync(backupPath, Encoding.UTF8);
            var backupData = JsonConvert.DeserializeObject<BackupData>(json);

            if (backupData == null || backupData.Credentials == null)
            {
                Console.WriteLine("Invalid backup file format");
                return false;
            }

            // Restore credentials
            foreach (var credential in backupData.Credentials)
            {
                await _vaultService.AddCredentialAsync(credential);
            }

            // Restore configuration if available
            if (backupData.Configuration != null)
            {
                await _vaultService.UpdateConfigurationAsync(backupData.Configuration);
            }

            Console.WriteLine($"Restored {backupData.Credentials.Count} credentials from backup");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Restore from backup failed: {ex.Message}");
            return false;
        }
    }

    // Private helper methods

    private List<Credential> FilterCredentials(List<Credential> credentials, ExportOptions options)
    {
        var filtered = credentials.AsEnumerable();

        // Filter by IDs if specified
        if (options.CredentialIds != null && options.CredentialIds.Count > 0)
        {
            filtered = filtered.Where(c => options.CredentialIds.Contains(c.Id));
        }

        // Filter by favorites
        if (options.ExportOnlyFavorites)
        {
            filtered = filtered.Where(c => c.IsFavorite);
        }

        // Filter by modification date
        if (options.ExportOnlyModified && options.ModifiedSince.HasValue)
        {
            filtered = filtered.Where(c => c.ModifiedAt >= options.ModifiedSince.Value);
        }

        return filtered.ToList();
    }

    // Helper class for backup deserialization
    private class BackupData
    {
        public string? BackupVersion { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalCredentials { get; set; }
        public VaultConfiguration? Configuration { get; set; }
        public List<Credential>? Credentials { get; set; }
    }
}
