using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using SecureVault.Core.Interfaces;
using SecureVault.Core.Models;

namespace SecureVault.Core.Services.Import;

/// <summary>
/// Professional implementation for importing credentials from Chrome and Samsung Pass
/// </summary>
public class ImportService : IImportService
{
    public async Task<ImportResult> ImportFromChromeAsync(string filePath)
    {
        var result = new ImportResult { Source = "Chrome" };

        try
        {
            if (!File.Exists(filePath))
            {
                result.Errors.Add("File not found");
                return result;
            }

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null
            };

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, config);

            var records = csv.GetRecords<ChromePasswordRecord>().ToList();
            result.TotalRecords = records.Count;

            foreach (var record in records)
            {
                try
                {
                    var credential = new Credential
                    {
                        Title = ExtractDomainFromUrl(record.Url),
                        Username = record.Username ?? string.Empty,
                        Password = record.Password ?? string.Empty,
                        Url = record.Url ?? string.Empty,
                        Domain = ExtractDomainFromUrl(record.Url),
                        Source = "Chrome",
                        ImportedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        ModifiedAt = DateTime.UtcNow
                    };

                    result.ImportedCredentials.Add(credential);
                    result.SuccessfulImports++;
                }
                catch (Exception ex)
                {
                    result.FailedImports++;
                    result.Errors.Add($"Failed to import record: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Import failed: {ex.Message}");
        }

        return result;
    }

    public async Task<ImportResult> ImportFromSamsungPassAsync(string filePath)
    {
        var result = new ImportResult { Source = "Samsung Pass" };

        try
        {
            if (!File.Exists(filePath))
            {
                result.Errors.Add("File not found");
                return result;
            }

            // Samsung Pass can export to CSV or JSON format
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (extension == ".csv")
            {
                return await ImportSamsungPassCsvAsync(filePath);
            }
            else if (extension == ".json")
            {
                return await ImportSamsungPassJsonAsync(filePath);
            }
            else
            {
                result.Errors.Add("Unsupported file format. Expected .csv or .json");
                return result;
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Import failed: {ex.Message}");
        }

        return result;
    }

    public bool ValidateChromeFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            using var reader = new StreamReader(filePath);
            var firstLine = reader.ReadLine();

            // Chrome CSV has specific headers
            return firstLine != null && 
                   (firstLine.Contains("name,url,username,password") || 
                    firstLine.Contains("url,username,password"));
        }
        catch
        {
            return false;
        }
    }

    public bool ValidateSamsungPassFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            if (extension == ".csv")
            {
                using var reader = new StreamReader(filePath);
                var firstLine = reader.ReadLine();
                return firstLine != null && firstLine.Contains("site") && firstLine.Contains("username");
            }
            else if (extension == ".json")
            {
                var json = File.ReadAllText(filePath);
                return json.Contains("\"site\"") || json.Contains("\"website\"");
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public List<string> GetSupportedFormats()
    {
        return new List<string>
        {
            "Chrome Password Manager (CSV)",
            "Samsung Pass (CSV)",
            "Samsung Pass (JSON)"
        };
    }

    // Private helper methods

    private async Task<ImportResult> ImportSamsungPassCsvAsync(string filePath)
    {
        var result = new ImportResult { Source = "Samsung Pass" };

        try
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null
            };

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, config);

            var records = csv.GetRecords<SamsungPassRecord>().ToList();
            result.TotalRecords = records.Count;

            foreach (var record in records)
            {
                try
                {
                    var credential = new Credential
                    {
                        Title = record.Site ?? ExtractDomainFromUrl(record.Url ?? string.Empty),
                        Username = record.Username ?? string.Empty,
                        Password = record.Password ?? string.Empty,
                        Url = record.Url ?? string.Empty,
                        Domain = ExtractDomainFromUrl(record.Url ?? string.Empty),
                        Notes = record.Notes ?? string.Empty,
                        Source = "Samsung Pass",
                        ImportedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        ModifiedAt = DateTime.UtcNow
                    };

                    result.ImportedCredentials.Add(credential);
                    result.SuccessfulImports++;
                }
                catch (Exception ex)
                {
                    result.FailedImports++;
                    result.Errors.Add($"Failed to import record: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Import failed: {ex.Message}");
        }

        return result;
    }

    private async Task<ImportResult> ImportSamsungPassJsonAsync(string filePath)
    {
        var result = new ImportResult { Source = "Samsung Pass" };

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var records = JsonConvert.DeserializeObject<List<SamsungPassJsonRecord>>(json);

            if (records == null)
            {
                result.Errors.Add("Failed to parse JSON file");
                return result;
            }

            result.TotalRecords = records.Count;

            foreach (var record in records)
            {
                try
                {
                    var credential = new Credential
                    {
                        Title = record.Site ?? record.Website ?? ExtractDomainFromUrl(record.Url ?? string.Empty),
                        Username = record.Username ?? record.UserId ?? string.Empty,
                        Password = record.Password ?? string.Empty,
                        Url = record.Url ?? record.Website ?? string.Empty,
                        Domain = ExtractDomainFromUrl(record.Url ?? record.Website ?? string.Empty),
                        Notes = record.Notes ?? string.Empty,
                        Source = "Samsung Pass",
                        ImportedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        ModifiedAt = DateTime.UtcNow
                    };

                    result.ImportedCredentials.Add(credential);
                    result.SuccessfulImports++;
                }
                catch (Exception ex)
                {
                    result.FailedImports++;
                    result.Errors.Add($"Failed to import record: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Import failed: {ex.Message}");
        }

        return result;
    }

    private string ExtractDomainFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "Unknown";

        try
        {
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            var uri = new Uri(url);
            var host = uri.Host;

            // Remove www. prefix
            if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            {
                host = host.Substring(4);
            }

            return host;
        }
        catch
        {
            return url;
        }
    }

    // CSV Record classes for mapping
    private class ChromePasswordRecord
    {
        public string? Name { get; set; }
        public string? Url { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    private class SamsungPassRecord
    {
        public string? Site { get; set; }
        public string? Url { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Notes { get; set; }
    }

    private class SamsungPassJsonRecord
    {
        public string? Site { get; set; }
        public string? Website { get; set; }
        public string? Url { get; set; }
        public string? Username { get; set; }
        public string? UserId { get; set; }
        public string? Password { get; set; }
        public string? Notes { get; set; }
    }
}
