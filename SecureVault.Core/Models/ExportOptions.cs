namespace SecureVault.Core.Models;

/// <summary>
/// Options for exporting credentials
/// </summary>
public class ExportOptions
{
    public ExportFormat Format { get; set; } = ExportFormat.Csv;
    public bool IncludePasswords { get; set; } = true;
    public bool EncryptExport { get; set; } = false;
    public string? EncryptionPassword { get; set; }
    public bool ExportOnlyModified { get; set; } = false;
    public bool ExportOnlyFavorites { get; set; } = false;
    public DateTime? ModifiedSince { get; set; }
    public List<string>? CredentialIds { get; set; }
}

public enum ExportFormat
{
    Csv,
    Json,
    EncryptedJson
}
