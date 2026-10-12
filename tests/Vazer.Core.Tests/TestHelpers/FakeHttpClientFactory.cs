namespace Vazer.Core.Tests.TestHelpers;

/// <summary>Returns <see cref="HttpClient"/> instances over a fake handler. No network.</summary>
public sealed class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public FakeHttpClientFactory(HttpMessageHandler handler)
    {
        _handler = handler;
    }

    public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
}
