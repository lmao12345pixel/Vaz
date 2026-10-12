using System.Net;
using System.Text;

namespace Vazer.Core.Tests.TestHelpers;

/// <summary>
/// In-memory HTTP handler with per-route responders. No real network.
/// Key = "METHOD /path?query".
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _routes = new();

    /// <summary>All requests received, in order.</summary>
    public List<HttpRequestMessage> Requests { get; } = new();

    /// <summary>Last request body per route key (empty when no content).</summary>
    public Dictionary<string, string> RequestBodies { get; } = new();

    public void Map(HttpMethod method, string pathAndQuery, Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _routes[$"{method.Method} {pathAndQuery}"] = responder;
    }

    public void MapJson(HttpMethod method, string pathAndQuery, HttpStatusCode status, string json)
    {
        Map(method, pathAndQuery, _ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
    }

    public int CallCount(HttpMethod method, string pathAndQuery)
    {
        var key = $"{method.Method} {pathAndQuery}";
        return Requests.Count(r => $"{r.Method.Method} {r.RequestUri!.PathAndQuery}" == key);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var key = $"{request.Method.Method} {request.RequestUri!.PathAndQuery}";
        Requests.Add(request);
        RequestBodies[key] = request.Content == null
            ? ""
            : await request.Content.ReadAsStringAsync(ct);

        if (_routes.TryGetValue(key, out var responder))
        {
            var response = responder(request);
            response.RequestMessage ??= request;
            return response;
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request };
    }
}
