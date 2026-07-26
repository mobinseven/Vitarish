using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Tamin.Integration.Auth;
using Vitarish.Medical.Application.Abstractions.Prescriptions;
using Vitarish.Medical.Domain.Entities;
using Vitarish.Modules.Base.Core.Platform.Errors;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal sealed class TaminCredentialSessionStore(
    PrescriptionDbContext dbContext,
    TaminProviderSessionFactory sessionFactory,
    TaminTokenVault tokenVault,
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<TaminOptions> taminOptions,
    TaminErrorMapper errorMapper,
    ILogger<TaminCredentialSessionStore> logger) : ITaminCredentialSessionStore
{
    private readonly PrescriptionDbContext _dbContext = dbContext;
    private readonly TaminProviderSessionFactory _sessionFactory = sessionFactory;
    private readonly TaminTokenVault _tokenVault = tokenVault;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IOptionsMonitor<TaminOptions> _taminOptions = taminOptions;
    private readonly TaminErrorMapper _errorMapper = errorMapper;
    private readonly ILogger<TaminCredentialSessionStore> _logger = logger;

    public async Task<TaminSessionResult> AuthenticateAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default)
    {
        return await GetOrRefreshSessionAsync(doctorId, clinicId, cancellationToken);
    }

    public async Task<TaminSessionResult> GetOrRefreshSessionAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default)
    {
        DoctorProviderCredential? credential = await FindCredentialAsync(doctorId, clinicId, cancellationToken);
        if (credential is null)
        {
            return TaminSessionResult.Failure(MissingCredentialError(doctorId));
        }

        try
        {
            TaminCredentialAccessTokenProvider tokenProvider = CreateTokenProvider(credential);
            Uri soaBaseUri = RequireAbsoluteHttpsUri(
                _taminOptions.CurrentValue.EffectiveSoaBaseUrl,
                nameof(TaminOptions.SoaBaseUrl));
            _ = await tokenProvider.GetAuthorizationTokenAsync(soaBaseUri, cancellationToken: cancellationToken);
            TaminProviderSession session = _sessionFactory.Create(tokenProvider);
            return TaminSessionResult.Success(session, ResolveDoctorTaminId(credential), credential.ProviderUsername, credential.ProviderClinicIdentifier);
        }
        catch (TaminReauthorizationRequiredException ex)
        {
            _logger.LogInformation(ex, "Tamin authorization is required for doctor {DoctorId} in clinic {ClinicId}", doctorId, clinicId);
            return TaminSessionResult.Failure(AuthenticationRequiredError(ex.Message));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Tamin token refresh failed for doctor {DoctorId} in clinic {ClinicId}", doctorId, clinicId);
            return TaminSessionResult.Failure(_errorMapper.MapError(new ProviderErrorContext
            {
                Exception = ex,
                ProviderErrorMessage = ex.Message
            }));
        }
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
            Uri accountBaseUri = RequireAbsoluteHttpsUri(
                options.EffectiveAccountBaseUrl,
                nameof(TaminOptions.AccountBaseUrl));
            Uri redirectUri = RequireAbsoluteHttpsUri(
                options.OAuthRedirectUri,
                nameof(TaminOptions.OAuthRedirectUri));
            string clientId = Require(options.ApiKey, nameof(TaminOptions.ApiKey));
            string codeVerifier = TaminTokenProvider.CreateCodeVerifier();
            string state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            string codeChallenge = Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));

            _tokenVault.WriteAuthorizationState(
                credential,
                state,
                codeVerifier,
                DateTimeOffset.UtcNow.AddMinutes(10));
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            UriBuilder authorizationUri = new(new Uri(accountBaseUri, "/auth/server/authorize"))
            {
                Query = string.Join('&',
                    $"redirect_uri={WebUtility.UrlEncode(redirectUri.ToString())}",
                    $"code_challenge={WebUtility.UrlEncode(codeChallenge)}",
                    $"client_id={WebUtility.UrlEncode(clientId)}",
                    "response_type=code",
                    "code_challenge_method=S256",
                    $"state={WebUtility.UrlEncode(state)}")
            };
            return new TaminAuthorizationStartResult(authorizationUri.Uri, null);
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

        TaminOptions options = _taminOptions.CurrentValue;
        try
        {
            TaminDoctorTokenExchange exchange = new(
                _httpClientFactory.CreateClient("TaminOAuthClient"),
                RequireAbsoluteHttpsUri(options.TokenEndpoint, nameof(TaminOptions.TokenEndpoint)),
                options.EffectiveEndpoint == TaminEndpointMode.Sandbox);
            DoctorToken token = await exchange.ExchangeAuthorizationCodeAsync(
                new DoctorAuthorizationCode(
                    authorizationCode,
                    Require(options.ApiKey, nameof(TaminOptions.ApiKey)),
                    Require(options.Audience, nameof(TaminOptions.Audience)),
                    Require(options.OAuthRedirectUri, nameof(TaminOptions.OAuthRedirectUri)),
                    authorizationState.CodeVerifier),
                cancellationToken);
            _tokenVault.Write(credential, token.AccessToken, token.RefreshToken, token.ExpiresAt);
            credential.SessionId = null;
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            TaminCredentialAccessTokenProvider tokenProvider = CreateTokenProvider(credential);
            TaminProviderSession session = _sessionFactory.Create(tokenProvider);
            _logger.LogInformation(
                "Tamin OAuth authorization completed for doctor {DoctorId} in clinic {ClinicId}",
                credential.DoctorId,
                credential.ClinicId);
            return TaminSessionResult.Success(session, ResolveDoctorTaminId(credential), credential.ProviderUsername, credential.ProviderClinicIdentifier);
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

    private TaminCredentialAccessTokenProvider CreateTokenProvider(DoctorProviderCredential credential) => new(
        credential,
        _dbContext,
        _tokenVault,
        _httpClientFactory,
        _taminOptions);

    private static string Require(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException($"Tamin {name} is required.") : value;

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

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

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
