using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vazer.Core.Abstractions;
using Vazer.Core.Auth;
using Vazer.Core.Http;
using Vazer.Core.Instances;
using Vazer.Core.Java;
using Vazer.Core.Launch;
using Vazer.Core.Loaders;
using Vazer.Core.Loaders.Optifine;
using Vazer.Core.Modpacks;
using Vazer.Core.Modrinth;
using Vazer.Core.Mods;
using Vazer.Core.Store;

namespace Vazer.Core.Extensions;

/// <summary>
/// Extension methods for registering Minecraft launcher services.
/// </summary>
public static class MinecraftServiceCollectionExtensions
{
    /// <summary>
    /// Adds Minecraft launcher services to the DI container.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configureOptions">Optional options configuration.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddMinecraftLauncher(
        this IServiceCollection services,
        Action<MinecraftOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Options
        services.AddOptions<MinecraftOptions>()
            .Validate(o => !string.IsNullOrEmpty(o.RootPath), "RootPath is required")
            .Validate(o => o.MsalClientId.Length > 0 || o.OfflineDemoMode, "MsalClientId required unless OfflineDemoMode is true");

        // HTTP clients with timeout
        services.AddHttpClient<MsalAuthProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<JavaResolver>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(10);
        });

        // Phase 2 - Modrinth / OptiFine / mod browser.
        // "optifine" phải AllowAutoRedirect = false: adloadx trả HTML chứa link
        // downloadx, không phải redirect. Bật redirect sẽ bypass bước parse.
        services.AddHttpClient("modrinth-api", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient("modrinth-download", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(300);
        });

        services.AddHttpClient("optifine", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false
        });

        // Item 4: downloader dùng chung (gom 4 chỗ download trùng lặp).
        services.AddSingleton<IFileDownloader, FileDownloader>();

        // Core services
        services.AddSingleton<IAccountStore, AccountStore>();
        services.AddSingleton<IAuthProvider, OfflineAuthProvider>();
        services.AddSingleton<IAuthProvider, MsalAuthProvider>();
        services.AddSingleton<CompositeAuthProvider>();
        services.AddSingleton<IAdoptiumClient, AdoptiumClient>();
        services.AddSingleton<IJavaResolver, JavaResolver>();
        services.AddSingleton<INeoForgeInstallerService, NeoForgeInstallerService>();
        services.AddSingleton<LoaderInstallerRegistry>(sp =>
        {
            var registry = new LoaderInstallerRegistry();
            registry.Register("neoforge", sp.GetRequiredService<INeoForgeInstallerService>());
            return registry;
        });
        services.AddSingleton<IInstanceManager, InstanceManager>();
        services.AddSingleton<IGameLauncher, CmlGameLauncher>();

        // Phase 2 services
        services.AddSingleton<IModrinthClient, ModrinthClient>();
        services.AddSingleton<IModInstallService, ModInstallService>();
        services.AddSingleton<IMrpackImporter, MrpackImporter>();
        services.AddSingleton<IOptifineInstallerService>(sp => new OptifineInstallerService(
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<IOptions<MinecraftOptions>>(),
            sp.GetRequiredService<ILogger<OptifineInstallerService>>(),
            sp.GetRequiredService<IModrinthClient>()));

        return services;
    }
}
