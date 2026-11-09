using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using Tamin.Integration;
using Vitarish.Medical.Application.Abstractions.Prescriptions;
using Vitarish.Medical.Domain.Entities;
using Vitarish.Modules.Base.Core.Platform.Errors;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal sealed class TaminCredentialSessionStore(
    PrescriptionDbContext dbContext,
    TaminClientFactory clientFactory,
    TaminTokenVault tokenVault,
    IOptionsMonitor<TaminOptions> taminOptions,
    TaminCredentialRefreshCoordinator refreshCoordinator,
    TaminErrorMapper errorMapper,
    ILogger<TaminCredentialSessionStore> logger) : ITaminCredentialSessionStore
{
    private readonly PrescriptionDbContext _dbContext = dbContext;
    private readonly TaminClientFactory _clientFactory = clientFactory;
    private readonly TaminTokenVault _tokenVault = tokenVault;
    private readonly IOptionsMonitor<TaminOptions> _taminOptions = taminOptions;
    private readonly TaminCredentialRefreshCoordinator _refreshCoordinator = refreshCoordinator;
    private readonly TaminErrorMapper _errorMapper = errorMapper;
    private readonly ILogger<TaminCredentialSessionStore> _logger = logger;

    public async Task<TaminSessionResult> AuthenticateAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default)
    {
        return await GetOrRefreshSessionAsync(doctorId, clinicId, cancellationToken);
    }

    public async Task<TaminSessionResult> GetOrRefreshSessionAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default)
    {
        DoctorProviderCredential? credential = await FindCredentialAsync(doctorId, clinicId, cancellationToken);
        return credential is null
            ? TaminSessionResult.Failure(MissingCredentialError(doctorId))
            : CreateSession(credential);
    }

    public async Task<TaminSessionResult> GetOrRefreshSessionAsync(Guid credentialId, CancellationToken cancellationToken = default)
    {
        DoctorProviderCredential? credential = await FindCredentialAsync(credentialId, cancellationToken);
        return credential is null
            ? TaminSessionResult.Failure(MissingCredentialError(Guid.Empty))
            : CreateSession(credential);
    }

    public async Task<TaminAuthorizationStartResult> BeginAuthorizationAsync(
        Guid credentialId,
        CancellationToken cancellationToken = default)
    {
        DoctorProviderCredential? credential = await FindCredentialAsync(credentialId, cancellationToken);
        if (credential is null)
        {
            return new TaminAuthorizationStartResult(null, MissingCredentialError(Guid.Empty));
        }

        try
        {
            TaminOptions options = _taminOptions.CurrentValue;
            Uri redirectUri = RequireAbsoluteHttpsUri(
                options.OAuthRedirectUri,
                nameof(TaminOptions.OAuthRedirectUri));
            ITaminClient client = _clientFactory.Create(CreateTokenStore(credential));
            string codeVerifier = TaminClient.CreateCodeVerifier();
            string state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

            _tokenVault.WriteAuthorizationState(
                credential,
                state,
                codeVerifier,
                DateTimeOffset.UtcNow.AddMinutes(10));
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            return new TaminAuthorizationStartResult(
                client.CreateAuthorizationUri(
                    redirectUri.ToString(),
                    TaminClient.CreateCodeChallenge(codeVerifier),
                    state),
                null);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return new TaminAuthorizationStartResult(null, _errorMapper.MapError(new ProviderErrorContext
            {
                Exception = ex,
                ProviderErrorMessage = ex.Message
            }));
        }
    }

    public async Task<TaminSessionResult> CompleteAuthorizationAsync(
        Guid credentialId,
        string authorizationCode,
        string state,
        CancellationToken cancellationToken = default)
    {
        DoctorProviderCredential? credential = await FindCredentialAsync(credentialId, cancellationToken);
        if (credential is null)
        {
            return TaminSessionResult.Failure(MissingCredentialError(Guid.Empty));
        }

        TaminAuthorizationState? authorizationState = _tokenVault.ReadAuthorizationState(credential);
        if (authorizationState is null ||
            authorizationState.ExpiresAt <= DateTimeOffset.UtcNow ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(authorizationState.State),
                Encoding.UTF8.GetBytes(state)))
        {
            credential.SessionId = null;
            _ = await _dbContext.SaveChangesAsync(cancellationToken);
            return TaminSessionResult.Failure(AuthenticationRequiredError("Tamin OAuth state is invalid or expired."));
        }

        try
        {
            TaminOptions options = _taminOptions.CurrentValue;
            ITaminClient client = _clientFactory.Create(CreateTokenStore(credential));
            // The SDK exchanges the code and writes the token through the credential's token store.
            await client.CompleteAuthorizationAsync(
                authorizationCode,
                RequireAbsoluteHttpsUri(options.OAuthRedirectUri, nameof(TaminOptions.OAuthRedirectUri)).ToString(),
                authorizationState.CodeVerifier,
                cancellationToken);
            credential.SessionId = null;
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Tamin OAuth authorization completed for doctor {DoctorId} in clinic {ClinicId}",
                credential.DoctorId,
                credential.ClinicId);
            return TaminSessionResult.Success(client, ResolveDoctorTaminId(credential), credential.ProviderUsername, credential.ProviderClinicIdentifier, credential.ProviderDoctorMobile);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or ArgumentException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Tamin OAuth authorization failed for doctor {DoctorId}", credential.DoctorId);
            return TaminSessionResult.Failure(_errorMapper.MapError(new ProviderErrorContext
            {
                Exception = ex,
                ProviderErrorMessage = ex.Message
            }));
        }
    }

    /// <summary>
    /// A credential with no stored token needs a new login. The token itself is read and, when expired,
    /// refreshed lazily by the SDK on the first call, under the credential's refresh lease.
    /// </summary>
    private TaminSessionResult CreateSession(DoctorProviderCredential credential)
    {
        TaminStoredToken stored = _tokenVault.Read(credential);
        if (string.IsNullOrWhiteSpace(stored.AccessToken) && string.IsNullOrWhiteSpace(stored.RefreshToken))
        {
            _logger.LogInformation(
                "Tamin authorization is required for doctor {DoctorId} in clinic {ClinicId}",
                credential.DoctorId,
                credential.ClinicId);
            return TaminSessionResult.Failure(AuthenticationRequiredError("Doctor authorization must be completed again."));
        }

        try
        {
            return TaminSessionResult.Success(
                _clientFactory.Create(CreateTokenStore(credential)),
                ResolveDoctorTaminId(credential),
                credential.ProviderUsername,
                credential.ProviderClinicIdentifier,
                credential.ProviderDoctorMobile);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Tamin client could not be created for doctor {DoctorId} in clinic {ClinicId}", credential.DoctorId, credential.ClinicId);
            return TaminSessionResult.Failure(_errorMapper.MapError(new ProviderErrorContext
            {
                Exception = ex,
                ProviderErrorMessage = ex.Message
            }));
        }
    }

    private TaminCredentialTokenStore CreateTokenStore(DoctorProviderCredential credential) => new(
        credential,
        _dbContext,
        _tokenVault,
        _refreshCoordinator);

    private static Uri RequireAbsoluteHttpsUri(string? value, string name) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) && uri.Scheme == Uri.UriSchemeHttps
            ? uri
            : throw new InvalidOperationException($"Tamin {name} must be an absolute HTTPS URI.");

    private static string? ResolveDoctorTaminId(DoctorProviderCredential credential) =>
        string.IsNullOrWhiteSpace(credential.ProviderDoctorIdentifier)
            ? credential.ProviderUsername
            : credential.ProviderDoctorIdentifier;

    private async Task<DoctorProviderCredential?> FindCredentialAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken) => await _dbContext.DoctorProviderCredentials
            .Where(c => c.DoctorId == doctorId &&
                c.ProviderKey == TaminInsuranceAdapter.ProviderKeyConst &&
                c.ClinicId == clinicId)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<DoctorProviderCredential?> FindCredentialAsync(Guid credentialId, CancellationToken cancellationToken) =>
        await _dbContext.DoctorProviderCredentials.FirstOrDefaultAsync(
            credential => credential.Id == credentialId &&
                credential.ProviderKey == TaminInsuranceAdapter.ProviderKeyConst,
            cancellationToken);

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
