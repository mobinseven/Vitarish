using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Medical.Application.Abstractions.Prescriptions;
using Vitarish.Medical.Domain.Entities;
using Vorn.Tamin;
using Vitarish.Modules.Base.Core.Platform.Errors;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal sealed class TaminCredentialSessionStore(
    PrescriptionDbContext dbContext,
    ITaminSessionFactory sessionFactory,
    IOptionsMonitor<TaminOptions> taminOptions,
    TaminErrorMapper errorMapper,
    ILogger<TaminCredentialSessionStore> logger) : ITaminCredentialSessionStore
{
    private readonly PrescriptionDbContext _dbContext = dbContext;
    private readonly ITaminSessionFactory _sessionFactory = sessionFactory;
    private readonly IOptionsMonitor<TaminOptions> _taminOptions = taminOptions;
    private readonly TaminErrorMapper _errorMapper = errorMapper;
    private readonly ILogger<TaminCredentialSessionStore> _logger = logger;

    public async Task<TaminSessionResult> AuthenticateAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default)
    {
        DoctorProviderCredential? credential = await FindCredentialAsync(doctorId, clinicId, cancellationToken);
        return credential is null
            ? TaminSessionResult.Failure(MissingCredentialError(doctorId))
            : await AuthenticateInternalAsync(credential, doctorId, clinicId, cancellationToken);
    }

    public async Task<TaminSessionResult> GetOrRefreshSessionAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default)
    {
        DoctorProviderCredential? credential = await FindCredentialAsync(doctorId, clinicId, cancellationToken);
        if (credential is null)
        {
            return TaminSessionResult.Failure(MissingCredentialError(doctorId));
        }

        if (!string.IsNullOrWhiteSpace(credential.TokenSecretRef) &&
            credential.TokenExpiresAt > DateTime.UtcNow.Add(_taminOptions.CurrentValue.TokenSkew))
        {
            TaminSession session = _sessionFactory.CreateTokenSession(credential.TokenSecretRef);
            return TaminSessionResult.Success(session, ResolveDoctorTaminId(credential), credential.ProviderUsername, credential.ProviderClinicIdentifier);
        }

        return await AuthenticateInternalAsync(credential, doctorId, clinicId, cancellationToken);
    }

    private async Task<TaminSessionResult> AuthenticateInternalAsync(DoctorProviderCredential credential, Guid doctorId, Guid clinicId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(credential.ProviderUsername) || string.IsNullOrWhiteSpace(credential.CredentialSecretRef))
        {
            return TaminSessionResult.Failure(MissingCredentialError(doctorId));
        }

        try
        {
            TaminSession session = await _sessionFactory.CreatePasswordSessionAsync(
                credential.ProviderUsername,
                credential.CredentialSecretRef,
                cancellationToken);

            string? token = session.HttpClient.DefaultRequestHeaders.Authorization?.Parameter;
            if (string.IsNullOrWhiteSpace(token))
            {
                return TaminSessionResult.Failure(AuthenticationRequiredError("Tamin authentication returned no token."));
            }

            credential.TokenSecretRef = token;
            credential.SessionId = null;
            credential.TokenExpiresAt = DateTime.UtcNow.AddMinutes(30);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tamin authentication successful for doctor {DoctorId} in clinic {ClinicId}", doctorId, clinicId);
            return TaminSessionResult.Success(session, ResolveDoctorTaminId(credential), credential.ProviderUsername, credential.ProviderClinicIdentifier);
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

    private static string? ResolveDoctorTaminId(DoctorProviderCredential credential) =>
        string.IsNullOrWhiteSpace(credential.ProviderDoctorIdentifier)
            ? credential.ProviderUsername
            : credential.ProviderDoctorIdentifier;

    private async Task<DoctorProviderCredential?> FindCredentialAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken) => await _dbContext.DoctorProviderCredentials
            .Where(c => c.DoctorId == doctorId &&
                c.ProviderKey == TaminInsuranceAdapter.ProviderKeyConst &&
                c.ClinicId == clinicId)
            .FirstOrDefaultAsync(cancellationToken);

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
