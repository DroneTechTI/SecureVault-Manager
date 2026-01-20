namespace SecureVault.Core.Models;

/// <summary>
/// Represents a single credential entry (username/password for a service)
/// </summary>
public class Credential
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastPasswordChange { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsCompromised { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    
    // Analysis properties
    public int PasswordStrength { get; set; } // 0-100
    public bool IsWeak { get; set; }
    public bool IsDuplicate { get; set; }
    public int DuplicateCount { get; set; }
    
    // Import metadata
    public string Source { get; set; } = "Manual"; // Manual, Chrome, Samsung Pass
    public DateTime? ImportedAt { get; set; }
}
