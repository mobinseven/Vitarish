using System.Buffers;
using System.Net;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Vitarish.E2e.Infrastructure.Fixtures;

internal sealed class E2eBrowserIngressProxy : IDisposable
{
    private const string ConfigurationFileName = "csp-browser-proxy-ports.json";
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection", "Keep-Alive", "Proxy-Authenticate", "Proxy-Authorization",
        "TE", "Trailer", "Transfer-Encoding", "Upgrade", "Host", "Expect",
    };
    private static readonly HashSet<string> WebSocketHandshakeHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection", "Host", "Sec-WebSocket-Extensions", "Sec-WebSocket-Key",
        "Sec-WebSocket-Protocol", "Sec-WebSocket-Version", "Upgrade",
    };

    private readonly HttpClient _httpClient = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.None,
        ConnectTimeout = TimeSpan.FromSeconds(10),
        UseCookies = false,
        UseProxy = false,
    });
    private readonly IReadOnlyDictionary<int, int> _proxyPorts;

    private E2eBrowserIngressProxy(IReadOnlyDictionary<int, int> proxyPorts) => _proxyPorts = proxyPorts;

    public static E2eBrowserIngressProxy? TryCreate(string applicationBaseDirectory, string configuredUrls)
    {
        string configurationPath = Path.Combine(applicationBaseDirectory, "Infrastructure", "Fixtures", ConfigurationFileName);
        using JsonDocument configuration = JsonDocument.Parse(File.ReadAllText(configurationPath));
        JsonElement browserPortsByProxyPort = configuration.RootElement.GetProperty("browserPortsByProxyPort");
        Dictionary<int, int> proxyPorts = [];
        HashSet<int> configuredHttpsPorts = configuredUrls
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) ? uri : null)
            .Where(uri => uri is not null && uri.Scheme == Uri.UriSchemeHttps && uri.IsLoopback)
            .Select(uri => uri!.Port)
            .ToHashSet();

        foreach (JsonProperty port in browserPortsByProxyPort.EnumerateObject())
        {
            if (!int.TryParse(port.Name, out int proxyPort))
                throw new InvalidOperationException("The E2E browser proxy port configuration is invalid.");
            int browserPort = port.Value.GetInt32();
            if (browserPort is < 1024 or > 65535 || proxyPort is < 1024 or > 65535 || browserPort == proxyPort || !proxyPorts.TryAdd(browserPort, proxyPort))
                throw new InvalidOperationException("The E2E browser proxy port configuration is invalid.");
        }

        if (proxyPorts.Count != 6)
            throw new InvalidOperationException("The E2E browser proxy requires all six Web application ports.");

        int configuredCount = proxyPorts.Keys.Count(configuredHttpsPorts.Contains);
        if (configuredCount == 0) return null;
        if (configuredCount != proxyPorts.Count)
            throw new InvalidOperationException("All six secure E2E browser proxy listeners must be configured together.");

        if (proxyPorts.Keys.Intersect(proxyPorts.Values).Any() || proxyPorts.Values.Distinct().Count() != proxyPorts.Count)
            throw new InvalidOperationException("The E2E browser and HTTP proxy ports must be distinct.");

        return new E2eBrowserIngressProxy(proxyPorts);
    }

    public void Use(WebApplication app)
    {
        app.UseWebSockets();
        app.Use(async (context, next) =>
        {
            if (!_proxyPorts.TryGetValue(context.Connection.LocalPort, out int proxyPort))
            {
                await next(context);
                return;
            }

            using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(2));
            using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, timeout.Token);
            try
            {
                if (context.WebSockets.IsWebSocketRequest)
                    await ProxyWebSocketAsync(context, proxyPort, linked.Token);
                else
                    await ProxyHttpAsync(context, proxyPort, linked.Token);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // The browser closed its request; request cancellation stops the upstream operation.
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                if (!context.Response.HasStarted) context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
            }
            catch (HttpRequestException exception)
            {
                app.Logger.LogError(exception, "The E2E browser ingress could not reach its loopback proxy on port {ProxyPort}.", proxyPort);
                if (!context.Response.HasStarted) context.Response.StatusCode = StatusCodes.Status502BadGateway;
            }
            catch (WebSocketException exception)
            {
                app.Logger.LogError(exception, "The E2E browser ingress could not proxy a loopback WebSocket on port {ProxyPort}.", proxyPort);
                if (!context.Response.HasStarted) context.Response.StatusCode = StatusCodes.Status502BadGateway;
            }
        });
    }

    private async Task ProxyHttpAsync(HttpContext context, int proxyPort, CancellationToken cancellationToken)
    {
        string pathAndQuery = $"{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
        using HttpRequestMessage upstreamRequest = new(new HttpMethod(context.Request.Method), new Uri($"http://{IPAddress.Loopback}:{proxyPort}{pathAndQuery}", UriKind.Absolute))
        {
            Version = HttpVersion.Version11,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
        };

        bool hasBody = context.Request.ContentLength is > 0 || context.Request.Headers.ContainsKey("Content-Type");
        if (hasBody) upstreamRequest.Content = new StreamContent(context.Request.Body);

        foreach (KeyValuePair<string, StringValues> header in context.Request.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key)) continue;
            if (upstreamRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray())) continue;
            upstreamRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
        upstreamRequest.Headers.Host = $"127.0.0.1:{proxyPort}";

        using HttpResponseMessage upstreamResponse = await _httpClient.SendAsync(
            upstreamRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        context.Response.StatusCode = (int)upstreamResponse.StatusCode;
        CopyHeaders(upstreamResponse.Headers, context.Response.Headers);
        CopyHeaders(upstreamResponse.Content.Headers, context.Response.Headers);
        context.Response.Headers.Remove("transfer-encoding");
        await upstreamResponse.Content.CopyToAsync(context.Response.Body, cancellationToken);
    }

    private static async Task ProxyWebSocketAsync(HttpContext context, int proxyPort, CancellationToken cancellationToken)
    {
        Uri target = new($"ws://{IPAddress.Loopback}:{proxyPort}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}", UriKind.Absolute);
        using ClientWebSocket upstream = new();
        upstream.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
        foreach (string protocol in context.WebSockets.WebSocketRequestedProtocols) upstream.Options.AddSubProtocol(protocol);
        foreach (KeyValuePair<string, StringValues> header in context.Request.Headers)
        {
            if (WebSocketHandshakeHeaders.Contains(header.Key)) continue;
            upstream.Options.SetRequestHeader(header.Key, header.Value.ToString());
        }

        await upstream.ConnectAsync(target, cancellationToken);
        using WebSocket browser = await context.WebSockets.AcceptWebSocketAsync(upstream.SubProtocol);
        Task fromBrowser = RelayWebSocketAsync(browser, upstream, cancellationToken);
        Task toBrowser = RelayWebSocketAsync(upstream, browser, cancellationToken);
        await Task.WhenAll(fromBrowser, toBrowser);
    }

    private static async Task RelayWebSocketAsync(WebSocket source, WebSocket destination, CancellationToken cancellationToken)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
        try
        {
            while (source.State is WebSocketState.Open or WebSocketState.CloseSent)
            {
                ValueWebSocketReceiveResult result = await source.ReceiveAsync(buffer.AsMemory(), cancellationToken);
                if (result.MessageType is WebSocketMessageType.Close)
                {
                    if (destination.State is WebSocketState.Open or WebSocketState.CloseReceived)
                        await destination.CloseOutputAsync(source.CloseStatus ?? WebSocketCloseStatus.NormalClosure, source.CloseStatusDescription, cancellationToken);
                    return;
                }

                await destination.SendAsync(buffer.AsMemory(0, result.Count), result.MessageType, result.EndOfMessage, cancellationToken);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static void CopyHeaders(HttpHeaders source, IHeaderDictionary destination)
    {
        foreach (KeyValuePair<string, IEnumerable<string>> header in source)
        {
            if (HopByHopHeaders.Contains(header.Key)) continue;
            destination[header.Key] = new StringValues(header.Value.ToArray());
        }
    }

    public void Dispose() => _httpClient.Dispose();
}
