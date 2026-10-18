using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Http;
using Vazer.Core;
using Vazer.Core.Abstractions;
using Vazer.Core.Auth;
using Vazer.Core.Extensions;
using Vazer.Core.Http;
using Vazer.Core.Instances;
using Vazer.Core.Java;
using Vazer.Core.Launch;
using Vazer.Core.Loaders.Optifine;
using Vazer.Core.Modpacks;
using Vazer.Core.Modrinth;
using Vazer.Core.Mods;

namespace Vazer.Core.Tests;

/// <summary>
/// Item 5: DI wiring - AddMinecraftLauncher dang ky du service, options validate dung.
/// </summary>
public sealed class MinecraftServiceCollectionExtensionsTests : IDisposable
{
    private readonly string _tempRoot;

    public MinecraftServiceCollectionExtensionsTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "vaz-di-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, true); } catch { }
    }

    private static ServiceProvider Build(Action<MinecraftOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMinecraftLauncher(configure ?? (o =>
        {
            o.OfflineDemoMode = true;
        }));
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddMinecraftLauncher_ResolvesCoreServices()
    {
        using var sp = Build(o =>
        {
            o.RootPath = _tempRoot;
            o.OfflineDemoMode = true;
        });

        sp.GetService<IFileDownloader>().Should().NotBeNull();
        sp.GetService<IAccountStore>().Should().NotBeNull();
        sp.GetService<CompositeAuthProvider>().Should().NotBeNull();
        sp.GetService<IAdoptiumClient>().Should().NotBeNull();
        sp.GetService<IJavaResolver>().Should().NotBeNull();
        sp.GetService<IInstanceManager>().Should().NotBeNull();
        sp.GetService<IGameLauncher>().Should().NotBeNull();
        sp.GetService<IModrinthClient>().Should().NotBeNull();
        sp.GetService<IModInstallService>().Should().NotBeNull();
        sp.GetService<IMrpackImporter>().Should().NotBeNull();
    }

    [Fact]
    public void AddMinecraftLauncher_ResolvesMultipleAuthProviders()
    {
        using var sp = Build(o =>
        {
            o.RootPath = _tempRoot;
            o.OfflineDemoMode = true;
        });

        var providers = sp.GetServices<IAuthProvider>().ToList();
        providers.Should().Contain(p => p.Kind == AccountKind.Offline);
        providers.Should().Contain(p => p.Kind == AccountKind.Microsoft);
    }

    [Fact]
    public void AddMinecraftLauncher_OfflineDemo_AllowsEmptyClientId()
    {
        using var sp = Build(o =>
        {
            o.RootPath = _tempRoot;
            o.OfflineDemoMode = true;
            o.MsalClientId = "";
        });

        var options = sp.GetRequiredService<IOptions<MinecraftOptions>>().Value;
        options.OfflineDemoMode.Should().BeTrue();
    }

    [Fact]
    public void AddMinecraftLauncher_OnlineWithoutClientId_FailsValidation()
    {
        using var sp = Build(o =>
        {
            o.RootPath = _tempRoot;
            o.OfflineDemoMode = false;
            o.MsalClientId = "";
        });

        Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<MinecraftOptions>>().Value);
    }

    [Fact]
    public void AddMinecraftLauncher_ResolvesPhase2ServicesAndNamedClients()
    {
        using var sp = Build(o =>
        {
            o.RootPath = _tempRoot;
            o.OfflineDemoMode = true;
        });

        sp.GetService<IModrinthClient>().Should().NotBeNull();
        sp.GetService<IModInstallService>().Should().NotBeNull();
        sp.GetService<IMrpackImporter>().Should().NotBeNull();
        sp.GetService<IOptifineInstallerService>().Should().NotBeNull();

        var factory = sp.GetRequiredService<IHttpClientFactory>();

        using var api = factory.CreateClient("modrinth-api");
        api.Timeout.Should().Be(TimeSpan.FromSeconds(30));

        using var download = factory.CreateClient("modrinth-download");
        download.Timeout.Should().Be(TimeSpan.FromSeconds(300));

        using var optifine = factory.CreateClient("optifine");
        optifine.Timeout.Should().Be(TimeSpan.FromSeconds(60));
    }
}
