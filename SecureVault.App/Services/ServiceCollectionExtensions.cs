using Microsoft.Extensions.DependencyInjection;
using SecureVault.Core.Interfaces;
using SecureVault.Core.Services.Analysis;
using SecureVault.Core.Services.Encryption;
using SecureVault.Core.Services.Export;
using SecureVault.Core.Services.Generation;
using SecureVault.Core.Services.Import;
using SecureVault.Core.Services.Vault;
using SecureVault.App.ViewModels;
using System.IO;

namespace SecureVault.App.Services;

/// <summary>
/// Extension methods for configuring dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSecureVaultServices(this IServiceCollection services)
    {
        // Get vault path
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SecureVault");
        
        if (!Directory.Exists(appDataPath))
            Directory.CreateDirectory(appDataPath);

        var vaultPath = Path.Combine(appDataPath, "vault.db");

        // Core services
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IVaultService>(sp => 
            new VaultService(sp.GetRequiredService<IEncryptionService>(), vaultPath));
        services.AddSingleton<ICompromisedPasswordService, CompromisedPasswordService>();
        services.AddSingleton<IPasswordAnalysisService>(sp =>
            new PasswordAnalysisService(sp.GetRequiredService<ICompromisedPasswordService>()));
        services.AddSingleton<IPasswordGeneratorService, PasswordGeneratorService>();
        services.AddSingleton<IImportService, ImportService>();
        services.AddSingleton<IExportService>(sp =>
            new ExportService(
                sp.GetRequiredService<IEncryptionService>(),
                sp.GetRequiredService<IVaultService>()));

        // ViewModels
        services.AddTransient<AuthenticationViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<CredentialsViewModel>();
        services.AddSingleton<SettingsViewModel>();

        return services;
    }
}
