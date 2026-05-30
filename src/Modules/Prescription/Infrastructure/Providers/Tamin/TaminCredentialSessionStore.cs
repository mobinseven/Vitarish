using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Modules.Base.Platform.Errors;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Domain.Entities;
using Vitarish.Prescription.Infrastructure.Data;
using Vitarish.TaminInsurance.Client;

namespace Vitarish.Prescription.Infrastructure.Providers.Tamin;

internal sealed class TaminCredentialSessionStore : ITaminCredentialSessionStore
{
    private readonly PrescriptionDbContext _dbContext;
    private readonly ITaminInsuranceClient _client;
    private readonly TaminErrorMapper _errorMapper;
    private readonly ILogger<TaminCredentialSessionStore> _logger;
    private Guid? _providerId;

    public TaminCredentialSessionStore(
        PrescriptionDbContext dbContext,
        ITaminInsuranceClient client,
        TaminErrorMapper errorMapper,
        ILogger<TaminCredentialSessionStore> logger)
    {
        _dbContext = dbContext;
        _client = client;
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
            return TaminSessionResult.Success(new TaminSession { Token = credential.TokenSecretRef, SessionId = credential.SessionId });
        }

        return await AuthenticateInternalAsync(credential, doctorId, clinicId, cancellationToken);
    }

    private async Task<TaminSessionResult> AuthenticateInternalAsync(DoctorProviderCredential credential, Guid doctorId, Guid clinicId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(credential.ProviderUsername) || string.IsNullOrWhiteSpace(credential.CredentialSecretRef))
            return TaminSessionResult.Failure(MissingCredentialError(doctorId));

        try
        {
            var authResponse = await _client.AuthenticateAsync(new TaminAuthRequest
            {
                Username = credential.ProviderUsername,
                Password = credential.CredentialSecretRef
            }, cancellationToken);

            if (string.IsNullOrWhiteSpace(authResponse?.Token))
                return TaminSessionResult.Failure(AuthenticationRequiredError("Tamin authentication returned no token."));

            credential.TokenSecretRef = authResponse.Token;
            credential.SessionId = authResponse.SessionId;
            credential.TokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(authResponse.ExpiresInMinutes ?? 30);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tamin authentication successful for doctor {DoctorId} in clinic {ClinicId}", doctorId, clinicId);
            return TaminSessionResult.Success(new TaminSession { Token = authResponse.Token, SessionId = authResponse.SessionId });
        }
        catch (TaminInsuranceClientException ex)
        {
            _logger.LogWarning(ex, "Tamin authentication failed for doctor {DoctorId} with status {StatusCode}", doctorId, ex.StatusCode);
            return TaminSessionResult.Failure(_errorMapper.MapError(new ProviderErrorContext
            {
                HttpStatusCode = (int)ex.StatusCode,
                ProviderErrorCode = ((int)ex.StatusCode).ToString(),
                ProviderErrorMessage = ex.ResponseBody
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
