using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Modules.Base.Platform.Errors;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Domain.Entities;
using Vitarish.Prescription.Infrastructure.Data;
using Vorn.Tamin;

namespace Vitarish.Prescription.Infrastructure.Providers.Tamin;

internal sealed class TaminCredentialSessionStore : ITaminCredentialSessionStore
{
    private readonly PrescriptionDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<TaminOptions> _taminOptions;
    private readonly TaminErrorMapper _errorMapper;
    private readonly ILogger<TaminCredentialSessionStore> _logger;
    private Guid? _providerId;

    public TaminCredentialSessionStore(
        PrescriptionDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<TaminOptions> taminOptions,
        TaminErrorMapper errorMapper,
        ILogger<TaminCredentialSessionStore> logger)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _taminOptions = taminOptions;
        _errorMapper = errorMapper;
        _logger = logger;
    }

    public async Task<TaminSessionResult> AuthenticateAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default)
    {
        var credential = await FindCredentialAsync(doctorId, cancellationToken);
        if (credential is null)
            return TaminSessionResult.Failure(MissingCredentialError(doctorId));

        return await AuthenticateInternalAsync(credential, doctorId, clinicId, cancellationToken);
    }

    public async Task<TaminSessionResult> GetOrRefreshSessionAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default)
    {
        var credential = await FindCredentialAsync(doctorId, cancellationToken);
        if (credential is null)
            return TaminSessionResult.Failure(MissingCredentialError(doctorId));

        if (!string.IsNullOrWhiteSpace(credential.TokenSecretRef) &&
            credential.TokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(2))
        {
            var options = _taminOptions.CurrentValue;
            var httpClient = _httpClientFactory.CreateClient("TaminClient");
            var session = new TaminSession(httpClient, credential.TokenSecretRef,
                new Uri(options.EffectiveBaseUrl), needToken: true, clientId: options.ApiKey);
            return TaminSessionResult.Success(session, credential.ProviderUsername);
        }

        return await AuthenticateInternalAsync(credential, doctorId, clinicId, cancellationToken);
    }

    private async Task<TaminSessionResult> AuthenticateInternalAsync(DoctorProviderCredential credential, Guid doctorId, Guid clinicId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(credential.ProviderUsername) || string.IsNullOrWhiteSpace(credential.CredentialSecretRef))
            return TaminSessionResult.Failure(MissingCredentialError(doctorId));

        var options = _taminOptions.CurrentValue;
        var httpClient = _httpClientFactory.CreateClient("TaminClient");
        var baseUri = new Uri(options.EffectiveBaseUrl);

        try
        {
            var session = await TaminSession.CreateAsync(
                httpClient,
                username: credential.ProviderUsername,
                password: credential.CredentialSecretRef,
                clientId: options.ApiKey,
                baseUri: baseUri,
                cancellationToken: cancellationToken);

            var token = session.HttpClient.DefaultRequestHeaders.Authorization?.Parameter;
            if (string.IsNullOrWhiteSpace(token))
                return TaminSessionResult.Failure(AuthenticationRequiredError("Tamin authentication returned no token."));

            credential.TokenSecretRef = token;
            credential.SessionId = null;
            credential.TokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tamin authentication successful for doctor {DoctorId} in clinic {ClinicId}", doctorId, clinicId);
            return TaminSessionResult.Success(session, credential.ProviderUsername);
        }
        catch (ConnectionError ex)
        {
            _logger.LogWarning(ex, "Tamin authentication failed for doctor {DoctorId} with status {StatusCode}", doctorId, ex.StatusCode);
            return TaminSessionResult.Failure(_errorMapper.MapError(new ProviderErrorContext
            {
                HttpStatusCode = ex.StatusCode.HasValue ? (int)ex.StatusCode.Value : null,
                ProviderErrorCode = ex.StatusCode.HasValue ? ((int)ex.StatusCode.Value).ToString() : null,
                ProviderErrorMessage = ex.Content
            }));
        }
        catch (UserLoginException ex)
        {
            _logger.LogWarning(ex, "Tamin authentication login rejected for doctor {DoctorId}: {Reason}", doctorId, ex.ReasonText);
            return TaminSessionResult.Failure(_errorMapper.MapError(new ProviderErrorContext
            {
                HttpStatusCode = ex.Status,
                ProviderErrorCode = ex.Status?.ToString(),
                ProviderErrorMessage = ex.ReasonText ?? ex.Message
            }));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Tamin authentication network error for doctor {DoctorId}", doctorId);
            return TaminSessionResult.Failure(_errorMapper.MapError(new ProviderErrorContext
            {
                Exception = ex,
                ProviderErrorMessage = ex.Message
            }));
        }
    }

    private async Task<DoctorProviderCredential?> FindCredentialAsync(Guid doctorId, CancellationToken cancellationToken)
    {
        var providerId = await GetProviderIdAsync(cancellationToken);
        if (providerId == Guid.Empty)
            return null;

        return await _dbContext.DoctorProviderCredentials
            .Where(c => c.DoctorId == doctorId && c.InsuranceProviderId == providerId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Guid> GetProviderIdAsync(CancellationToken cancellationToken)
    {
        if (_providerId.HasValue)
            return _providerId.Value;

        _providerId = await _dbContext.InsuranceProviders
            .AsNoTracking()
            .Where(p => p.ProviderKey == TaminInsuranceAdapter.ProviderKeyConst)
            .Select(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return _providerId.Value;
    }

    private static IntegrationError MissingCredentialError(Guid doctorId) => new()
    {
        Category = ErrorCategory.Authentication,
        UserMessage = "اطلاعات ورود پزشک برای تامین اجتماعی یافت نشد.",
        TechnicalDetail = $"No credential found for doctor {doctorId} with provider TAMIN",
        IsRetryable = false
    };

    private static IntegrationError AuthenticationRequiredError(string technicalDetail) => new()
    {
        Category = ErrorCategory.Authentication,
        UserMessage = "نیاز به ورود مجدد به سامانه تامین اجتماعی است.",
        TechnicalDetail = technicalDetail,
        IsRetryable = false
    };
}
