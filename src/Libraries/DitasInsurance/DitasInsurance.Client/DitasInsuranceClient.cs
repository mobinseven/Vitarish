using System.Net.Http.Json;

namespace Vitarish.DitasInsurance.Client;

public sealed class DitasInsuranceClient : IDitasInsuranceClient
{
    private const string ApiKeyHeader = "X-Api-Key";
    private readonly HttpClient _httpClient;
    private readonly DitasInsuranceClientOptions _options;

    public DitasInsuranceClient(HttpClient httpClient, DitasInsuranceClientOptions options)
    {
        _httpClient = httpClient;
        _options = options;

        if (string.IsNullOrWhiteSpace(_options.EffectiveBaseUrl))
            throw new InvalidOperationException("DITAS base URL is required.");
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("DITAS API key is required.");

        if (_httpClient.BaseAddress is null)
            _httpClient.BaseAddress = new Uri(_options.EffectiveBaseUrl, UriKind.Absolute);
    }

    public async Task CheckHealthAsync(CancellationToken cancellationToken = default)
        => _ = await SendAsync<object>(HttpMethod.Get, "api/health", content: null, idempotencyKey: null, correlationId: null, cancellationToken);

    public Task<DitasEntitlementResponse?> CheckEntitlementAsync(string nationalId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nationalId);
        return SendAsync<DitasEntitlementResponse>(HttpMethod.Get, $"api/entitlement/{Uri.EscapeDataString(nationalId)}", content: null, idempotencyKey: null, correlationId: null, cancellationToken);
    }

    public Task<DitasSubmissionResponse?> SubmitPrescriptionAsync(DitasPrescriptionSubmitRequest request, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        return SendAsync<DitasSubmissionResponse>(HttpMethod.Post, "api/prescription/submit", request, idempotencyKey, correlationId: null, cancellationToken);
    }

    public Task<DitasFetchResponse?> FetchPrescriptionAsync(string remotePrescriptionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(remotePrescriptionId);
        return SendAsync<DitasFetchResponse>(HttpMethod.Get, $"api/prescription/{Uri.EscapeDataString(remotePrescriptionId)}", content: null, idempotencyKey: null, correlationId: null, cancellationToken);
    }

    public async Task CancelPrescriptionAsync(string remotePrescriptionId, string correlationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(remotePrescriptionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        _ = await SendAsync<object>(HttpMethod.Delete, $"api/prescription/{Uri.EscapeDataString(remotePrescriptionId)}", content: null, idempotencyKey: null, correlationId, cancellationToken);
    }

    private async Task<T?> SendAsync<T>(HttpMethod method, string url, object? content, string? idempotencyKey, string? correlationId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Add(ApiKeyHeader, _options.ApiKey);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            request.Headers.Add("X-Idempotency-Key", idempotencyKey);
        if (!string.IsNullOrWhiteSpace(correlationId))
            request.Headers.Add("X-Correlation-Id", correlationId);
        if (content is not null)
            request.Content = JsonContent.Create(content);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new DitasInsuranceClientException(response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));

        if (response.Content.Headers.ContentLength == 0)
            return default;

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }
}
