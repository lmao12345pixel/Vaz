using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using Vazer.Core.Launch;

namespace Vazer.Core.Tests;

public class LauncherNetworkResilienceTests
{
    [Fact]
    public void IsNetworkException_HttpRequestException_ReturnsTrue()
    {
        var ex = new HttpRequestException("An error occurred while sending the request.");
        CmlGameLauncher.IsNetworkException(ex).Should().BeTrue();
    }

    [Fact]
    public void IsNetworkException_SocketException_ReturnsTrue()
    {
        var ex = new SocketException((int)SocketError.ConnectionReset);
        CmlGameLauncher.IsNetworkException(ex).Should().BeTrue();
    }

    [Fact]
    public void IsNetworkException_TimeoutException_ReturnsTrue()
    {
        var ex = new TimeoutException("The operation has timed out.");
        CmlGameLauncher.IsNetworkException(ex).Should().BeTrue();
    }

    [Fact]
    public void IsNetworkException_TaskCanceledException_Timeout_ReturnsTrue()
    {
        var ex = new TaskCanceledException("A task was canceled.");
        CmlGameLauncher.IsNetworkException(ex).Should().BeTrue();
    }

    [Fact]
    public void IsNetworkException_WrappedInAggregateException_ReturnsTrue()
    {
        var inner = new HttpRequestException("Connection refused");
        var agg = new AggregateException("One or more errors occurred.", inner);
        CmlGameLauncher.IsNetworkException(agg).Should().BeTrue();
    }

    [Fact]
    public void IsNetworkException_WrappedInGenericExceptionWithSendingRequestMessage_ReturnsTrue()
    {
        var ex = new Exception("Download failed: An error occurred while sending the request. Please check connection.");
        CmlGameLauncher.IsNetworkException(ex).Should().BeTrue();
    }

    [Fact]
    public void IsNetworkException_IOException_ReturnsTrue()
    {
        var ex = new IOException("Unable to read data from the transport connection.");
        CmlGameLauncher.IsNetworkException(ex).Should().BeTrue();
    }

    [Fact]
    public void IsNetworkException_ArgumentException_ReturnsFalse()
    {
        var ex = new ArgumentException("Invalid path specified");
        CmlGameLauncher.IsNetworkException(ex).Should().BeFalse();
    }

    [Fact]
    public void IsNetworkException_Null_ReturnsFalse()
    {
        CmlGameLauncher.IsNetworkException(null!).Should().BeFalse();
    }
}
