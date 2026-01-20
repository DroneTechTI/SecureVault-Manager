using System.Security.Cryptography;
using System.Text;
using SecureVault.Core.Interfaces;

namespace SecureVault.Core.Services.Analysis;

/// <summary>
/// Service for checking compromised passwords using Have I Been Pwned API with k-anonymity
/// </summary>
public class CompromisedPasswordService : ICompromisedPasswordService
{
    private const string HibpApiUrl = "https://api.pwnedpasswords.com/range/";
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    static CompromisedPasswordService()
    {
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "SecureVault-Manager");
    }

    public async Task<(bool IsCompromised, int TimesCompromised)> CheckPasswordAsync(string password)
    {
        if (string.IsNullOrEmpty(password))
            return (false, 0);

        try
        {
            // Hash the password with SHA-1
            var hash = ComputeSha1Hash(password);
            
            // Get the first 5 characters (k-anonymity prefix)
            var prefix = hash.Substring(0, 5);
            var suffix = hash.Substring(5);

            // Query the API with the prefix
            var response = await HttpClient.GetStringAsync(HibpApiUrl + prefix);

            // Parse the response
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var parts = line.Split(':');
                if (parts.Length == 2)
                {
                    var hashSuffix = parts[0].Trim();
                    
                    // Check if this hash matches our password
                    if (string.Equals(hashSuffix, suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(parts[1].Trim(), out int count))
                        {
                            return (true, count);
                        }
                    }
                }
            }

            // Password not found in breaches
            return (false, 0);
        }
        catch (HttpRequestException)
        {
            // Network error - assume not compromised but log the error
            Console.WriteLine("Unable to check password against breach database (network error)");
            return (false, 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking password: {ex.Message}");
            return (false, 0);
        }
    }

    public async Task<Dictionary<string, (bool IsCompromised, int TimesCompromised)>> CheckPasswordsAsync(List<string> passwords)
    {
        var results = new Dictionary<string, (bool IsCompromised, int TimesCompromised)>();

        // Group passwords by hash prefix to minimize API calls
        var passwordsByPrefix = passwords
            .Distinct()
            .GroupBy(p => GetHashPrefix(p))
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (prefix, passwordGroup) in passwordsByPrefix)
        {
            try
            {
                // Query the API once for all passwords with the same prefix
                var response = await HttpClient.GetStringAsync(HibpApiUrl + prefix);
                var breaches = ParseHibpResponse(response);

                foreach (var password in passwordGroup)
                {
                    var hash = ComputeSha1Hash(password);
                    var suffix = hash.Substring(5);

                    if (breaches.TryGetValue(suffix, out int count))
                    {
                        results[password] = (true, count);
                    }
                    else
                    {
                        results[password] = (false, 0);
                    }
                }

                // Rate limiting - be nice to the API
                await Task.Delay(100);
            }
            catch
            {
                // If API call fails, mark all passwords in this group as unchecked
                foreach (var password in passwordGroup)
                {
                    results[password] = (false, 0);
                }
            }
        }

        return results;
    }

    public string GetHashPrefix(string password)
    {
        if (string.IsNullOrEmpty(password))
            return string.Empty;

        var hash = ComputeSha1Hash(password);
        return hash.Substring(0, 5);
    }

    // Private helper methods

    private string ComputeSha1Hash(string input)
    {
        using var sha1 = SHA1.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha1.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
    }

    private Dictionary<string, int> ParseHibpResponse(string response)
    {
        var breaches = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var parts = line.Split(':');
            if (parts.Length == 2)
            {
                var hashSuffix = parts[0].Trim();
                if (int.TryParse(parts[1].Trim(), out int count))
                {
                    breaches[hashSuffix] = count;
                }
            }
        }

        return breaches;
    }
}
