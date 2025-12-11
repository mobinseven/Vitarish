using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using System.Net;
using Vitarish.Modules.Instagram.Scraper;
using Vitarish.Modules.Instagram.Scraper.Models;

namespace Vitarish.Instagram.Scraper;

public sealed class InstagramScraperClient : IDisposable, IInstagramScraperApiClient
{
    internal const string DefaultBaseUrl = "https://api.apify.com/v2";

    private readonly string _apifyToken;
    private readonly InstagramScraper _api;
    private readonly ILogger<InstagramScraperClient> _logger;
    private readonly RetryOptions _retryOptions;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    public InstagramScraperClient(string apifyToken, HttpClient? httpClient = null, ILogger<InstagramScraperClient>? logger = null, RetryOptions? retryOptions = null)
    {
        if (string.IsNullOrWhiteSpace(apifyToken))
        {
            throw new ArgumentException("Apify token must be provided.", nameof(apifyToken));
        }

        _apifyToken = apifyToken;
        _logger = logger ?? NullLogger<InstagramScraperClient>.Instance;
        _retryOptions = retryOptions ?? RetryOptions.Default;

        _httpClient = httpClient ?? CreateDefaultHttpClient();
        _ownsHttpClient = httpClient is null;

        HttpClientRequestAdapter adapter = new(new AnonymousAuthenticationProvider(), httpClient: _httpClient)
        {
            BaseUrl = DefaultBaseUrl,
        };

        _api = new InstagramScraper(adapter);
    }

    public async Task<DatasetItemsResponse> RunScraperAndGetDatasetItemsAsync(InputSchema input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var stream = await ExecuteWithRetryAsync(
            () => _api.Acts.ApifyInstagramScraper.RunSyncGetDatasetItems.PostAsync(
                input,
                request => request.QueryParameters.Token = _apifyToken,
                ct),
            nameof(RunScraperAndGetDatasetItemsAsync),
            ct).ConfigureAwait(false);

        return await DatasetItemsResponse.FromStreamAsync(stream, ct).ConfigureAwait(false);
    }

    public async Task<RunInfoResponse> StartScraperRunAsync(InputSchema input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var response = await ExecuteWithRetryAsync(
            () => _api.Acts.ApifyInstagramScraper.Runs.PostAsync(
                input,
                request => request.QueryParameters.Token = _apifyToken,
                ct),
            nameof(StartScraperRunAsync),
            ct).ConfigureAwait(false);

        return response?.Data is null
            ? throw new InvalidOperationException("The Instagram scraper run response was empty.")
            : new RunInfoResponse(response);
    }

    public async Task<KeyValueStoreOutput> RunScraperSyncAsync(InputSchema input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var stream = await ExecuteWithRetryAsync(
            () => _api.Acts.ApifyInstagramScraper.RunSync.PostAsync(
                input,
                request => request.QueryParameters.Token = _apifyToken,
                ct),
            nameof(RunScraperSyncAsync),
            ct).ConfigureAwait(false);

        return await KeyValueStoreOutput.FromStreamAsync(stream, ct).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private static HttpClient CreateDefaultHttpClient()
    {
        SocketsHttpHandler handler = new()
        {
            AutomaticDecompression = DecompressionMethods.All,
        };

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromMinutes(35),
        };
    }

    private static bool IsRetriableStatus(int? statusCode) => statusCode is (int)HttpStatusCode.TooManyRequests or >= (int)HttpStatusCode.InternalServerError;

    private TimeSpan NextDelay(TimeSpan currentDelay)
    {
        TimeSpan next = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * 2);
        return next > _retryOptions.MaxDelay ? _retryOptions.MaxDelay : next;
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, CancellationToken ct)
    {
        var attempt = 0;
        var delay = _retryOptions.InitialDelay;

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            attempt++;

            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (ApiException ex) when (attempt < _retryOptions.MaxAttempts && IsRetriableStatus(ex.ResponseStatusCode))
            {
                await WaitAndLogAsync(operationName, attempt, delay, ex.Message, ct).ConfigureAwait(false);
                delay = NextDelay(delay);
            }
            catch (HttpRequestException ex) when (attempt < _retryOptions.MaxAttempts)
            {
                await WaitAndLogAsync(operationName, attempt, delay, ex.Message, ct).ConfigureAwait(false);
                delay = NextDelay(delay);
            }
        }
    }

    private async Task WaitAndLogAsync(string operationName, int attempt, TimeSpan delay, string errorMessage, CancellationToken ct)
    {
        _logger.LogWarning(
            "InstagramScraperClient operation {OperationName} failed on attempt {Attempt}. Retrying after {Delay}. Error: {Error}",
            operationName,
            attempt,
            delay,
            errorMessage);

        await Task.Delay(delay, ct).ConfigureAwait(false);
    }
}
