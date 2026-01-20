using Windows.Storage;

namespace SecureVault.App.Services;

/// <summary>
/// Service for managing application language (Italian/English)
/// </summary>
public class LocalizationService
{
    private const string LanguageKey = "AppLanguage";
    private string _currentLanguage;

    public LocalizationService()
    {
        _currentLanguage = LoadLanguage();
    }

    public string CurrentLanguage => _currentLanguage;

    public bool IsItalian => _currentLanguage == "it";
    public bool IsEnglish => _currentLanguage == "en";

    public string LoadLanguage()
    {
        var saved = ApplicationData.Current.LocalSettings.Values[LanguageKey]?.ToString();
        return saved ?? "it"; // Default: Italian
    }

    public void SetLanguage(string language)
    {
        if (language != "it" && language != "en")
            language = "it";

        _currentLanguage = language;
        ApplicationData.Current.LocalSettings.Values[LanguageKey] = language;
    }

    public string Get(string englishText)
    {
        if (IsEnglish)
            return englishText;

        // Italian translations
        return englishText switch
        {
            // Authentication
            "SecureVault Manager" => "SecureVault Manager",
            "Your passwords, secured locally" => "Le tue password, protette localmente",
            "Unlock Vault" => "Sblocca Vault",
            "Master Password" => "Password Principale",
            "Enter your master password" => "Inserisci la tua password principale",
            "Confirm Password" => "Conferma Password",
            "Confirm your master password" => "Conferma la tua password principale",
            "Create Master Password" => "Crea Password Principale",
            "Create new vault" => "Crea nuovo vault",
            "Back to unlock" => "Torna allo sblocco",
            "Create Vault" => "Crea Vault",
            
            // Navigation
            "Dashboard" => "Dashboard",
            "My Credentials" => "Le Mie Password",
            "Import" => "Importa",
            "Export" => "Esporta",
            "Security" => "Sicurezza",
            "Settings" => "Impostazioni",
            "Password Generator" => "Generatore Password",
            "Lock Vault" => "Blocca Vault",
            "Create Backup" => "Crea Backup",
            
            // Dashboard
            "Security Dashboard" => "Dashboard Sicurezza",
            "Password Health Overview" => "Panoramica Salute Password",
            "Total Credentials" => "Credenziali Totali",
            "Strong Passwords" => "Password Forti",
            
            // Credentials
            "Search credentials..." => "Cerca credenziali...",
            "All" => "Tutte",
            
            // Generator
            "Click Generate to create a password" => "Clicca Genera per creare una password",
            "Generate" => "Genera",
            "Copy" => "Copia",
            "Options" => "Opzioni",
            "Password Length" => "Lunghezza Password",
            "Include Uppercase (A-Z)" => "Includi Maiuscole (A-Z)",
            "Include Lowercase (a-z)" => "Includi Minuscole (a-z)",
            "Include Numbers (0-9)" => "Includi Numeri (0-9)",
            "Include Special Characters" => "Includi Caratteri Speciali (!@#$)",
            "Avoid Ambiguous Characters" => "Evita Caratteri Ambigui (0,O,1,l)",
            
            // Import
            "Import Passwords" => "Importa Password",
            "Import your existing passwords from Chrome or Samsung Pass" => "Importa le tue password esistenti da Chrome o Samsung Pass",
            "Select Chrome CSV File" => "Seleziona File CSV Chrome",
            "Select Samsung Pass File" => "Seleziona File Samsung Pass",
            "Import Results" => "Risultati Importazione",
            
            // Export
            "Export Passwords" => "Esporta Password",
            "Export your credentials in various formats" => "Esporta le tue credenziali in vari formati",
            "Export to CSV" => "Esporta in CSV",
            "Export Encrypted JSON" => "Esporta JSON Cifrato",
            "Create Full Backup" => "Backup Completo",
            
            // Security
            "Security Center" => "Centro Sicurezza",
            "Run Full Analysis" => "Avvia Analisi Completa",
            "Check Compromised Passwords" => "Verifica Password Compromesse",
            "Find Weak Passwords" => "Trova Password Deboli",
            "Find Duplicate Passwords" => "Trova Password Duplicate",
            
            // Settings
            "Auto-lock timeout (minutes)" => "Blocco automatico dopo (minuti)",
            "Clipboard clear timeout (seconds)" => "Cancella appunti dopo (secondi)",
            "Check for compromised passwords" => "Verifica password compromesse",
            "Change Master Password" => "Cambia Password Principale",
            "Enable automatic backups" => "Abilita backup automatici",
            "About" => "Informazioni",
            "Save Settings" => "Salva Impostazioni",
            "Version" => "Versione",
            
            // Password Status (singular and plural)
            "Strong" => "Forte",
            "Weak" => "Debole",
            "Duplicate" => "Duplicata",
            "Compromised" => "Compromessa",
            "Weak Passwords" => "Password Deboli",
            "Duplicate Passwords" => "Password Duplicate",
            
            // Common
            "Close" => "Chiudi",
            "Cancel" => "Annulla",
            "OK" => "OK",
            "Yes" => "Sì",
            "No" => "No",
            "Error" => "Errore",
            "Success" => "Successo",
            "Warning" => "Avviso",
            "Loading..." => "Caricamento...",
            
            _ => englishText // Fallback to English
        };
    }
}
