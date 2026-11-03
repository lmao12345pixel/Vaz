using System.IO;
using CmlLib.Core;
using FluentAssertions;
using Vazer.Core.Launch;
using Xunit;

namespace Vazer.Core.Tests;

public class GameLauncherPathTests
{
    [Fact]
    public void CreateInstanceMinecraftPath_RedirectsSharedDirectoriesToSharedRoot()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "vaz_test_root_" + Path.GetRandomFileName());
        var tempInstance = Path.Combine(tempRoot, "instances", "test_instance");
        var tempShared = Path.Combine(tempRoot, "instances");

        try
        {
            var sharedPath = new MinecraftPath(tempShared);
            var instanceMcPath = CmlGameLauncher.CreateInstanceMinecraftPath(tempInstance, sharedPath);

            // BasePath or BaseDir points to the specific instance
            instanceMcPath.BasePath.Should().Be(tempInstance);

            // Shared directories point to shared root, NOT instance directory
            instanceMcPath.Library.Should().Be(sharedPath.Library);
            instanceMcPath.Assets.Should().Be(sharedPath.Assets);
            instanceMcPath.Versions.Should().Be(sharedPath.Versions);
            instanceMcPath.Runtime.Should().Be(sharedPath.Runtime);

            instanceMcPath.Library.Should().NotContain(tempInstance);
            instanceMcPath.Assets.Should().NotContain(tempInstance);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                try { Directory.Delete(tempRoot, true); } catch { }
            }
        }
    }
}
