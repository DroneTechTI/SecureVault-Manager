namespace SecureVault.Core.Models;

/// <summary>
/// Result of importing credentials from external source
/// </summary>
public class ImportResult
{
    public int TotalRecords { get; set; }
    public int SuccessfulImports { get; set; }
    public int FailedImports { get; set; }
    public int SkippedDuplicates { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<Credential> ImportedCredentials { get; set; } = new();
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = string.Empty;
    
    public bool IsSuccessful => SuccessfulImports > 0 && Errors.Count == 0;
}
