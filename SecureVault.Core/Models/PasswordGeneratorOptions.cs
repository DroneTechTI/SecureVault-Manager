namespace SecureVault.Core.Models;

/// <summary>
/// Options for password generation
/// </summary>
public class PasswordGeneratorOptions
{
    public int Length { get; set; } = 16;
    public bool UseUppercase { get; set; } = true;
    public bool UseLowercase { get; set; } = true;
    public bool UseDigits { get; set; } = true;
    public bool UseSpecialChars { get; set; } = true;
    public bool AvoidAmbiguous { get; set; } = true; // Avoid 0, O, I, l, etc.
    public bool RequireFromEachCategory { get; set; } = true;
    public string CustomCharacters { get; set; } = string.Empty;
    public int MinimumStrength { get; set; } = 80; // Minimum strength score (0-100)
    
    // Passphrase options
    public bool IsPassphrase { get; set; }
    public int WordCount { get; set; } = 4;
    public string Separator { get; set; } = "-";
    public bool CapitalizeWords { get; set; } = true;
    public bool IncludeNumber { get; set; } = true;
}
