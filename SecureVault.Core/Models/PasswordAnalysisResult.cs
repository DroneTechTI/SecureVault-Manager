namespace SecureVault.Core.Models;

/// <summary>
/// Result of password analysis for a single credential
/// </summary>
public class PasswordAnalysisResult
{
    public string CredentialId { get; set; } = string.Empty;
    public int Strength { get; set; } // 0-100
    public bool IsWeak { get; set; }
    public bool IsDuplicate { get; set; }
    public int DuplicateCount { get; set; }
    public bool IsCompromised { get; set; }
    public int TimesCompromised { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Details about password strength calculation
/// </summary>
public class PasswordStrengthDetails
{
    public int Score { get; set; } // 0-100
    public int Length { get; set; }
    public bool HasUppercase { get; set; }
    public bool HasLowercase { get; set; }
    public bool HasDigits { get; set; }
    public bool HasSpecialChars { get; set; }
    public double Entropy { get; set; }
    public bool HasCommonPatterns { get; set; }
    public bool IsInDictionary { get; set; }
    public string StrengthLevel { get; set; } = string.Empty; // Weak, Fair, Good, Strong, Excellent
}
