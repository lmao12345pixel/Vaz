using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vazer.Core;
using Vazer.Core.Loaders;
using Vazer.Core.Tests.TestHelpers;

namespace Vazer.Core.Tests;

public sealed class ForgeInstallerTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly FakeHttpMessageHandler _http;

    public ForgeInstallerTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "vaz-forge-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _http = new FakeHttpMessageHandler();
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, true); } catch { }
    }

    private ForgeInstaller CreateService(ProcessRunnerFunc? processRunner = null)
    {
        var options = Options.Create(new MinecraftOptions { RootPath = _tempRoot });
        return new ForgeInstaller(
            new FakeHttpClientFactory(_http),
            NullLogger<ForgeInstaller>.Instance,
            options,
            javaResolver: null,
            processRunner: processRunner);
    }

    [Theory]
    [InlineData("1.13", true)]
    [InlineData("1.16.5", true)]
    [InlineData("1.20.1", true)]
    [InlineData("26.3", true)]
    [InlineData("1.12.2", false)]
    [InlineData("1.8.9", false)]
    [InlineData("1.7.10", false)]
    [InlineData("24w14a", false)]
    [InlineData("", false)]
    public void SupportsVersion_And_IsHeadlessSupported_ReturnsExpected(string mc, bool expected)
    {
        var svc = CreateService();
        svc.SupportsVersion(mc).Should().Be(expected);
        ForgeInstaller.IsHeadlessSupported(mc).Should().Be(expected);
    }

    [Theory]
    [InlineData("1.12.2")]
    [InlineData("1.8.9")]
    [InlineData("1.7.10")]
    public async Task InstallAsync_ThrowsNotSupported_WhenMcBelow113(string mc)
    {
        var svc = CreateService();
        var act = async () => await svc.InstallAsync(Path.Combine(_tempRoot, "inst"), mc, "some-ver", null, CancellationToken.None);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*Forge headless chỉ hỗ trợ Minecraft 1.13+*");
    }

    [Fact]
    public async Task InstallAsync_EnsuresLauncherProfiles_AndRunsHeadlessCommand()
    {
        ProcessStartInfo? capturedPsi = null;
        var instancePath = Path.Combine(_tempRoot, "inst-1.20.1");

        // Process runner giả lập chạy thành công và tạo version json
        var svc = CreateService((psi, ct) =>
        {
            capturedPsi = psi;
            var vDir = Path.Combine(instancePath, "versions", "1.20.1-forge-47.2.0");
            Directory.CreateDirectory(vDir);
            File.WriteAllText(Path.Combine(vDir, "1.20.1-forge-47.2.0.json"), "{}");
            return Task.FromResult(new ProcessRunResult(0, "Successfully installed client", ""));
        });

        // Tạo sẵn file dummy installer jar để không gọi network
        var downloadDir = Path.Combine(_tempRoot, "java", "downloads");
        Directory.CreateDirectory(downloadDir);
        File.WriteAllText(Path.Combine(downloadDir, "forge-1.20.1-47.2.0-installer.jar"), "dummy");

        await svc.InstallAsync(instancePath, "1.20.1", "1.20.1-47.2.0", null, CancellationToken.None);

        // Verify launcher_profiles.json được tạo
        File.Exists(Path.Combine(instancePath, "launcher_profiles.json")).Should().BeTrue();

        // Verify command arguments
        capturedPsi.Should().NotBeNull();
        capturedPsi!.Arguments.Should().Contain("-jar ");
        capturedPsi.Arguments.Should().Contain("forge-1.20.1-47.2.0-installer.jar");
        capturedPsi.Arguments.Should().Contain("--installClient");
        capturedPsi.Arguments.Should().Contain(instancePath);

        // Verify instance.json được tạo cho standalone call
        File.Exists(Path.Combine(instancePath, "instance.json")).Should().BeTrue();
    }

    [Fact]
    public async Task InstallAsync_ThrowsInvalidOperationException_WhenProcessFails()
    {
        var instancePath = Path.Combine(_tempRoot, "inst-fail");
        var svc = CreateService((psi, ct) =>
            Task.FromResult(new ProcessRunResult(1, "There was an error during installation", "Error: corrupt jar")));

        var downloadDir = Path.Combine(_tempRoot, "java", "downloads");
        Directory.CreateDirectory(downloadDir);
        File.WriteAllText(Path.Combine(downloadDir, "forge-1.20.1-47.2.0-installer.jar"), "dummy");

        var act = async () => await svc.InstallAsync(instancePath, "1.20.1", "1.20.1-47.2.0", null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Không thể cài đặt Forge*");
    }

    [Fact]
    public async Task InstallAsync_PrependsMcVersion_IfOnlyForgeVersionProvided()
    {
        ProcessStartInfo? capturedPsi = null;
        var instancePath = Path.Combine(_tempRoot, "inst-prepend");

        var svc = CreateService((psi, ct) =>
        {
            capturedPsi = psi;
            var vDir = Path.Combine(instancePath, "versions", "forge-1.20.1-47.2.0");
            Directory.CreateDirectory(vDir);
            File.WriteAllText(Path.Combine(vDir, "forge.json"), "{}");
            return Task.FromResult(new ProcessRunResult(0, "Installed", ""));
        });

        var downloadDir = Path.Combine(_tempRoot, "java", "downloads");
        Directory.CreateDirectory(downloadDir);
        File.WriteAllText(Path.Combine(downloadDir, "forge-1.20.1-47.2.0-installer.jar"), "dummy");

        // Truyền "47.2.0" không có prefix "1.20.1-"
        await svc.InstallAsync(instancePath, "1.20.1", "47.2.0", null, CancellationToken.None);

        capturedPsi.Should().NotBeNull();
        capturedPsi!.Arguments.Should().Contain("forge-1.20.1-47.2.0-installer.jar");
    }
}
