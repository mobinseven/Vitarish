using Microsoft.Extensions.Logging;
using Vitarish.Modules.Base.Platform.Errors;
using Vitarish.Modules.Base.Platform.Transactions;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Infrastructure.Providers;

/// <summary>
/// Decorator that wraps an adapter with resilience (timeout, retry, circuit-breaker),
/// centralized error mapping, and transaction logging.
/// </summary>
public sealed class ResilientAdapterDecorator(
    IInsuranceProviderAdapter inner,
    IProviderErrorMapper errorMapper,
    IProviderResiliencePipelineProvider resilienceProvider,
    IExternalApiTransactionLogger transactionLogger,
    ILogger<ResilientAdapterDecorator> logger) : IInsuranceProviderAdapter
{
    private readonly IInsuranceProviderAdapter _inner = inner;
    private readonly IProviderErrorMapper _errorMapper = errorMapper;
    private readonly IProviderResiliencePipelineProvider _resilienceProvider = resilienceProvider;
    private readonly IExternalApiTransactionLogger _transactionLogger = transactionLogger;
    private readonly ILogger<ResilientAdapterDecorator> _logger = logger;

    public string ProviderKey => _inner.ProviderKey;

    public IReadOnlySet<InsuranceProviderCapabilityType> GetCapabilities() => _inner.GetCapabilities();

    public Task<AdapterResult> AuthenticateAsync(AuthenticateRequest request, CancellationToken cancellationToken = default)
        => ExecuteWithResilienceAsync(
            nameof(AuthenticateAsync),
            Guid.NewGuid().ToString(),
            ct => _inner.AuthenticateAsync(request, ct),
            cancellationToken);

    public Task<AdapterResult<EntitlementResponse>> CheckEntitlementAsync(EntitlementRequest request, CancellationToken cancellationToken = default)
        => ExecuteWithResilienceAsync(
            nameof(CheckEntitlementAsync),
            Guid.NewGuid().ToString(),
            ct => _inner.CheckEntitlementAsync(request, ct),
            cancellationToken);

    public Task<AdapterResult<SubmissionResponse>> SubmitPrescriptionAsync(SubmissionRequest request, CancellationToken cancellationToken = default)
        => ExecuteWithResilienceAsync(
            nameof(SubmitPrescriptionAsync),
            request.CorrelationId,
            ct => _inner.SubmitPrescriptionAsync(request, ct),
            cancellationToken);

    public Task<AdapterResult<FetchResponse>> FetchPrescriptionAsync(FetchRequest request, CancellationToken cancellationToken = default)
        => ExecuteWithResilienceAsync(
            nameof(FetchPrescriptionAsync),
            request.CorrelationId ?? Guid.NewGuid().ToString(),
            ct => _inner.FetchPrescriptionAsync(request, ct),
            cancellationToken);

    public Task<AdapterResult<SubmissionResponse>> EditPrescriptionAsync(EditRequest request, CancellationToken cancellationToken = default)
        => ExecuteWithResilienceAsync(
            nameof(EditPrescriptionAsync),
            request.CorrelationId,
            ct => _inner.EditPrescriptionAsync(request, ct),
            cancellationToken);

    public Task<AdapterResult> CancelPrescriptionAsync(CancelRequest request, CancellationToken cancellationToken = default)
        => ExecuteWithResilienceAsync(
            nameof(CancelPrescriptionAsync),
            request.CorrelationId,
            ct => _inner.CancelPrescriptionAsync(request, ct),
            cancellationToken);

    public Task<AdapterResult<ReferenceDataResponse>> FetchReferenceDataAsync(ReferenceDataRequest request, CancellationToken cancellationToken = default)
        => ExecuteWithResilienceAsync(
            nameof(FetchReferenceDataAsync),
            Guid.NewGuid().ToString(),
            ct => _inner.FetchReferenceDataAsync(request, ct),
            cancellationToken);

    private async Task<AdapterResult> ExecuteWithResilienceAsync(
        string operationName,
        string correlationId,
        Func<CancellationToken, Task<AdapterResult>> operation,
        CancellationToken cancellationToken)
    {
        ProviderResilienceOptions options = _resilienceProvider.GetOptions(ProviderKey);
        ApiTransaction transaction = CreateTransaction(operationName, correlationId);
        Guid transactionId = await _transactionLogger.BeginTransactionAsync(transaction, cancellationToken);

        int attempt = 0;
        while (true)
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(options.Timeout);

                AdapterResult result = await operation(cts.Token);

                await _transactionLogger.CompleteTransactionAsync(
                    transactionId, responseCode: 200, responseMessage: null,
                    success: result.IsSuccess || result.IsNotSupported,
                    cancellationToken: cancellationToken);

                return result;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                attempt++;
                if (attempt >= options.MaxRetryAttempts)
                {
                    IntegrationError error = _errorMapper.MapError(new ProviderErrorContext
                    {
                        OperationName = operationName,
                        Exception = new TimeoutException($"Operation timed out after {options.Timeout}")
                    });
                    await _transactionLogger.CompleteTransactionAsync(
                        transactionId, responseCode: 408, responseMessage: "Timeout",
                        success: false, errorCategory: error.Category.ToString(),
                        cancellationToken: cancellationToken);
                    return AdapterResult.Failure(error);
                }

                TimeSpan delay = CalculateBackoff(attempt, options);
                _logger.LogWarning("Provider {ProviderKey} operation {Operation} timed out, retry {Attempt}/{Max} after {Delay}ms",
                    ProviderKey, operationName, attempt, options.MaxRetryAttempts, delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                attempt++;
                if (attempt >= options.MaxRetryAttempts)
                {
                    IntegrationError error = _errorMapper.MapError(new ProviderErrorContext
                    {
                        HttpStatusCode = (int?)ex.StatusCode,
                        ProviderErrorMessage = ex.Message,
                        OperationName = operationName,
                        Exception = ex
                    });
                    await _transactionLogger.CompleteTransactionAsync(
                        transactionId, responseCode: (int?)ex.StatusCode, responseMessage: ex.Message,
                        success: false, errorCategory: error.Category.ToString(),
                        cancellationToken: cancellationToken);
                    return AdapterResult.Failure(error);
                }

                TimeSpan delay = CalculateBackoff(attempt, options);
                _logger.LogWarning(ex, "Provider {ProviderKey} operation {Operation} failed with HTTP error, retry {Attempt}/{Max} after {Delay}ms",
                    ProviderKey, operationName, attempt, options.MaxRetryAttempts, delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                IntegrationError error = _errorMapper.MapError(new ProviderErrorContext
                {
                    ProviderErrorMessage = ex.Message,
                    OperationName = operationName,
                    Exception = ex
                });
                await _transactionLogger.CompleteTransactionAsync(
                    transactionId, responseCode: null, responseMessage: ex.Message,
                    success: false, errorCategory: error.Category.ToString(),
                    cancellationToken: cancellationToken);
                _logger.LogError(ex, "Provider {ProviderKey} operation {Operation} failed with unhandled error",
                    ProviderKey, operationName);
                return AdapterResult.Failure(error);
            }
        }
    }

    private async Task<AdapterResult<T>> ExecuteWithResilienceAsync<T>(
        string operationName,
        string correlationId,
        Func<CancellationToken, Task<AdapterResult<T>>> operation,
        CancellationToken cancellationToken)
    {
        ProviderResilienceOptions options = _resilienceProvider.GetOptions(ProviderKey);
        ApiTransaction transaction = CreateTransaction(operationName, correlationId);
        Guid transactionId = await _transactionLogger.BeginTransactionAsync(transaction, cancellationToken);

        int attempt = 0;
        while (true)
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(options.Timeout);

                AdapterResult<T> result = await operation(cts.Token);

                await _transactionLogger.CompleteTransactionAsync(
                    transactionId, responseCode: 200, responseMessage: null,
                    success: result.IsSuccess || result.IsNotSupported,
                    cancellationToken: cancellationToken);

                return result;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                attempt++;
                if (attempt >= options.MaxRetryAttempts)
                {
                    IntegrationError error = _errorMapper.MapError(new ProviderErrorContext
                    {
                        OperationName = operationName,
                        Exception = new TimeoutException($"Operation timed out after {options.Timeout}")
                    });
                    await _transactionLogger.CompleteTransactionAsync(
                        transactionId, responseCode: 408, responseMessage: "Timeout",
                        success: false, errorCategory: error.Category.ToString(),
                        cancellationToken: cancellationToken);
                    return AdapterResult<T>.Failure(error);
                }

                TimeSpan delay = CalculateBackoff(attempt, options);
                _logger.LogWarning("Provider {ProviderKey} operation {Operation} timed out, retry {Attempt}/{Max} after {Delay}ms",
                    ProviderKey, operationName, attempt, options.MaxRetryAttempts, delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                attempt++;
                if (attempt >= options.MaxRetryAttempts)
                {
                    IntegrationError error = _errorMapper.MapError(new ProviderErrorContext
                    {
                        HttpStatusCode = (int?)ex.StatusCode,
                        ProviderErrorMessage = ex.Message,
                        OperationName = operationName,
                        Exception = ex
                    });
                    await _transactionLogger.CompleteTransactionAsync(
                        transactionId, responseCode: (int?)ex.StatusCode, responseMessage: ex.Message,
                        success: false, errorCategory: error.Category.ToString(),
                        cancellationToken: cancellationToken);
                    return AdapterResult<T>.Failure(error);
                }

                TimeSpan delay = CalculateBackoff(attempt, options);
                _logger.LogWarning(ex, "Provider {ProviderKey} operation {Operation} failed with HTTP error, retry {Attempt}/{Max} after {Delay}ms",
                    ProviderKey, operationName, attempt, options.MaxRetryAttempts, delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                IntegrationError error = _errorMapper.MapError(new ProviderErrorContext
                {
                    ProviderErrorMessage = ex.Message,
                    OperationName = operationName,
                    Exception = ex
                });
                await _transactionLogger.CompleteTransactionAsync(
                    transactionId, responseCode: null, responseMessage: ex.Message,
                    success: false, errorCategory: error.Category.ToString(),
                    cancellationToken: cancellationToken);
                _logger.LogError(ex, "Provider {ProviderKey} operation {Operation} failed with unhandled error",
                    ProviderKey, operationName);
                return AdapterResult<T>.Failure(error);
            }
        }
    }

    private ApiTransaction CreateTransaction(string operationName, string correlationId) => new()
    {
        ProviderKey = ProviderKey,
        OperationName = operationName,
        CorrelationId = correlationId,
        StartedAt = DateTime.UtcNow
    };

    private static TimeSpan CalculateBackoff(int attempt, ProviderResilienceOptions options)
    {
        TimeSpan delay = options.RetryBaseDelay * Math.Pow(2, attempt - 1);
        return delay > options.RetryMaxDelay ? options.RetryMaxDelay : delay;
    }
}
