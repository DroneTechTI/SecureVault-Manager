using System.Text.RegularExpressions;
using SecureVault.Core.Interfaces;
using SecureVault.Core.Models;

namespace SecureVault.Core.Services.Analysis;

/// <summary>
/// Professional password analysis service for security assessment
/// </summary>
public class PasswordAnalysisService : IPasswordAnalysisService
{
    private readonly ICompromisedPasswordService _compromisedPasswordService;

    // Common weak passwords and patterns
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "123456", "12345678", "qwerty", "abc123", "monkey", "letmein",
        "password1", "123456789", "12345", "1234567", "qwertyuiop", "admin", "welcome",
        "login", "passw0rd", "master", "hello", "freedom", "whatever", "trustno1"
    };

    private static readonly string[] CommonPatterns = new[]
    {
        @"^(.)\1+$", // Repeated characters (aaaa, 1111)
        @"^(012|123|234|345|456|567|678|789|890)+", // Sequential numbers
        @"^(abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|uvw|vwx|wxy|xyz)+", // Sequential letters
        @"^(qwert|asdf|zxcv)", // Keyboard patterns
        @"^\d{4,}$", // Only numbers (4+ digits)
        @"^[a-z]+$", // Only lowercase
        @"^[A-Z]+$" // Only uppercase
    };

    public PasswordAnalysisService(ICompromisedPasswordService compromisedPasswordService)
    {
        _compromisedPasswordService = compromisedPasswordService;
    }

    public async Task<PasswordAnalysisResult> AnalyzeCredentialAsync(Credential credential)
    {
        var result = new PasswordAnalysisResult
        {
            CredentialId = credential.Id,
            AnalyzedAt = DateTime.UtcNow
        };

        // Calculate strength
        var strengthDetails = CalculatePasswordStrength(credential.Password);
        result.Strength = strengthDetails.Score;
        result.IsWeak = strengthDetails.Score < 60;

        // Check if compromised
        try
        {
            var (isCompromised, timesCompromised) = await _compromisedPasswordService.CheckPasswordAsync(credential.Password);
            result.IsCompromised = isCompromised;
            result.TimesCompromised = timesCompromised;

            if (isCompromised)
            {
                result.Issues.Add($"This password has been found in {timesCompromised:N0} data breaches");
                result.Recommendations.Add("Change this password immediately");
            }
        }
        catch
        {
            // If HIBP check fails, continue with other analysis
        }

        // Identify issues and recommendations
        if (strengthDetails.Length < 8)
        {
            result.Issues.Add("Password is too short (less than 8 characters)");
            result.Recommendations.Add("Use at least 12-16 characters");
        }

        if (!strengthDetails.HasUppercase)
        {
            result.Issues.Add("No uppercase letters");
            result.Recommendations.Add("Add uppercase letters (A-Z)");
        }

        if (!strengthDetails.HasLowercase)
        {
            result.Issues.Add("No lowercase letters");
            result.Recommendations.Add("Add lowercase letters (a-z)");
        }

        if (!strengthDetails.HasDigits)
        {
            result.Issues.Add("No numbers");
            result.Recommendations.Add("Add numbers (0-9)");
        }

        if (!strengthDetails.HasSpecialChars)
        {
            result.Issues.Add("No special characters");
            result.Recommendations.Add("Add special characters (!@#$%^&*)");
        }

        if (strengthDetails.HasCommonPatterns)
        {
            result.Issues.Add("Contains common patterns");
            result.Recommendations.Add("Avoid predictable patterns");
        }

        if (strengthDetails.IsInDictionary)
        {
            result.Issues.Add("Uses common dictionary words");
            result.Recommendations.Add("Use random characters or passphrases");
        }

        if (result.Issues.Count == 0 && !result.IsCompromised && result.Strength >= 80)
        {
            result.Recommendations.Add("This is a strong password - keep it secure!");
        }

        return result;
    }

    public async Task<List<PasswordAnalysisResult>> AnalyzeAllCredentialsAsync(List<Credential> credentials)
    {
        var results = new List<PasswordAnalysisResult>();

        // First pass: analyze individual passwords
        var analysisTask = credentials.Select(c => AnalyzeCredentialAsync(c)).ToList();
        results = (await Task.WhenAll(analysisTask)).ToList();

        // Second pass: check for duplicates
        var duplicates = FindDuplicatePasswords(credentials);
        
        foreach (var (password, creds) in duplicates)
        {
            if (creds.Count > 1)
            {
                foreach (var cred in creds)
                {
                    var result = results.FirstOrDefault(r => r.CredentialId == cred.Id);
                    if (result != null)
                    {
                        result.IsDuplicate = true;
                        result.DuplicateCount = creds.Count;
                        result.Issues.Add($"This password is reused on {creds.Count} accounts");
                        result.Recommendations.Add("Use a unique password for each account");
                    }
                }
            }
        }

        return results;
    }

    public PasswordStrengthDetails CalculatePasswordStrength(string password)
    {
        var details = new PasswordStrengthDetails
        {
            Length = password?.Length ?? 0
        };

        if (string.IsNullOrEmpty(password))
        {
            details.Score = 0;
            details.StrengthLevel = "None";
            return details;
        }

        // Check character types
        details.HasUppercase = password.Any(char.IsUpper);
        details.HasLowercase = password.Any(char.IsLower);
        details.HasDigits = password.Any(char.IsDigit);
        details.HasSpecialChars = password.Any(c => !char.IsLetterOrDigit(c));

        // Check for common patterns and dictionary words
        details.HasCommonPatterns = HasCommonPatterns(password);
        details.IsInDictionary = IsInCommonDictionary(password);

        // Calculate entropy
        details.Entropy = CalculateEntropy(password);

        // Calculate score (0-100)
        int score = 0;

        // Length contribution (0-40 points)
        score += Math.Min(40, details.Length * 2);

        // Character variety (0-30 points)
        if (details.HasUppercase) score += 8;
        if (details.HasLowercase) score += 8;
        if (details.HasDigits) score += 7;
        if (details.HasSpecialChars) score += 7;

        // Entropy bonus (0-20 points)
        score += (int)Math.Min(20, details.Entropy / 3);

        // Penalties
        if (details.HasCommonPatterns) score -= 20;
        if (details.IsInDictionary) score -= 25;
        if (details.Length < 8) score -= 20;

        // Ensure score is in valid range
        details.Score = Math.Clamp(score, 0, 100);

        // Determine strength level
        details.StrengthLevel = details.Score switch
        {
            >= 90 => "Excellent",
            >= 70 => "Strong",
            >= 50 => "Good",
            >= 30 => "Fair",
            _ => "Weak"
        };

        return details;
    }

    public Dictionary<string, List<Credential>> FindDuplicatePasswords(List<Credential> credentials)
    {
        var passwordGroups = credentials
            .Where(c => !string.IsNullOrEmpty(c.Password))
            .GroupBy(c => c.Password)
            .Where(g => g.Count() > 1)
            .ToDictionary(g => g.Key, g => g.ToList());

        return passwordGroups;
    }

    public List<Credential> FindWeakPasswords(List<Credential> credentials, int minimumStrength = 60)
    {
        var weakPasswords = new List<Credential>();

        foreach (var credential in credentials)
        {
            var strength = CalculatePasswordStrength(credential.Password);
            if (strength.Score < minimumStrength)
            {
                weakPasswords.Add(credential);
            }
        }

        return weakPasswords;
    }

    public bool HasCommonPatterns(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        foreach (var pattern in CommonPatterns)
        {
            if (Regex.IsMatch(password, pattern, RegexOptions.IgnoreCase))
                return true;
        }

        return false;
    }

    public bool IsInCommonDictionary(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        return CommonPasswords.Contains(password);
    }

    public async Task<SecurityScore> CalculateSecurityScoreAsync(List<Credential> credentials)
    {
        var score = new SecurityScore
        {
            TotalCredentials = credentials.Count,
            CalculatedAt = DateTime.UtcNow
        };

        if (credentials.Count == 0)
        {
            score.OverallScore = 0;
            return score;
        }

        // Analyze all credentials
        var analysisResults = await AnalyzeAllCredentialsAsync(credentials);

        score.WeakPasswords = analysisResults.Count(r => r.IsWeak);
        score.CompromisedPasswords = analysisResults.Count(r => r.IsCompromised);
        score.DuplicatePasswords = analysisResults.Count(r => r.IsDuplicate);
        score.StrongPasswords = analysisResults.Count(r => r.Strength >= 80 && !r.IsCompromised && !r.IsDuplicate);

        // Calculate passwords needing update
        score.PasswordsNeedingUpdate = analysisResults.Count(r => 
            r.IsWeak || r.IsCompromised || r.IsDuplicate);

        // Calculate overall score (0-100)
        int overallScore = 100;

        // Penalties
        double weakPenalty = (score.WeakPasswords / (double)score.TotalCredentials) * 30;
        double compromisedPenalty = (score.CompromisedPasswords / (double)score.TotalCredentials) * 40;
        double duplicatePenalty = (score.DuplicatePasswords / (double)score.TotalCredentials) * 30;

        overallScore -= (int)(weakPenalty + compromisedPenalty + duplicatePenalty);

        score.OverallScore = Math.Clamp(overallScore, 0, 100);

        return score;
    }

    // Private helper methods

    private double CalculateEntropy(string password)
    {
        if (string.IsNullOrEmpty(password))
            return 0;

        // Calculate character pool size
        int poolSize = 0;
        if (password.Any(char.IsLower)) poolSize += 26;
        if (password.Any(char.IsUpper)) poolSize += 26;
        if (password.Any(char.IsDigit)) poolSize += 10;
        if (password.Any(c => !char.IsLetterOrDigit(c))) poolSize += 32;

        // Entropy = log2(poolSize^length)
        return password.Length * Math.Log2(poolSize);
    }
}
