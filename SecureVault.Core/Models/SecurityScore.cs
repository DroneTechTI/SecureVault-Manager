namespace SecureVault.Core.Models;

/// <summary>
/// Represents the overall security health of the vault
/// </summary>
public class SecurityScore
{
    public int OverallScore { get; set; } // 0-100
    public int TotalCredentials { get; set; }
    public int WeakPasswords { get; set; }
    public int DuplicatePasswords { get; set; }
    public int CompromisedPasswords { get; set; }
    public int StrongPasswords { get; set; }
    public int PasswordsNeedingUpdate { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    
    public string GetScoreLevel()
    {
        return OverallScore switch
        {
            >= 90 => "Excellent",
            >= 70 => "Good",
            >= 50 => "Fair",
            >= 30 => "Poor",
            _ => "Critical"
        };
    }
    
    public string GetScoreColor()
    {
        return OverallScore switch
        {
            >= 90 => "#4CAF50", // Green
            >= 70 => "#8BC34A", // Light Green
            >= 50 => "#FFC107", // Amber
            >= 30 => "#FF9800", // Orange
            _ => "#F44336" // Red
        };
    }
}
