using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Authentication;
using Tamin.Integration.Auth;
using Vitarish.Medical.Domain.Entities.Prescriptions;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal sealed class TaminCredentialAccessTokenProvider(
    DoctorProviderCredential credential,
    PrescriptionDbContext dbContext,
    TaminTokenVault tokenVault,
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<TaminOptions> optionsMonitor,
    TaminCredentialRefreshCoordinator refreshCoordinator) : IAccessTokenProvider
{
    public AllowedHostsValidator AllowedHostsValidator { get; } =
        new(optionsMonitor.CurrentValue.AllowedHosts);

    public async Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedHostsValidator.IsUrlHostValid(uri))
        {
            return string.Empty;
        }

        await refreshCoordinator.WaitAsync(credential.Id, cancellationToken);
        try
        {
            await dbContext.Entry(credential).ReloadAsync(cancellationToken);
            TaminStoredToken stored = tokenVault.Read(credential);
            if (!string.IsNullOrWhiteSpace(stored.AccessToken) &&
                stored.ExpiresAt > DateTimeOffset.UtcNow.Add(optionsMonitor.CurrentValue.TokenSkew))
            {
                return stored.AccessToken;
            }

            if (string.IsNullOrWhiteSpace(stored.RefreshToken))
            {
                throw new TaminReauthorizationRequiredException("Doctor authorization must be completed again.");
            }

            TaminOptions options = optionsMonitor.CurrentValue;
            TaminDoctorTokenExchange exchange = new(
                httpClientFactory.CreateClient("TaminOAuthClient"),
                RequireAbsoluteHttpsUri(options.TokenEndpoint, nameof(options.TokenEndpoint)),
                options.EffectiveEndpoint == TaminEndpointMode.Sandbox);

            DoctorToken refreshed;
            try
            {
                refreshed = await exchange.RefreshAsync(
                    stored.RefreshToken,
                    Require(options.ApiKey, nameof(options.ApiKey)),
                    Require(options.Audience, nameof(options.Audience)),
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new TaminReauthorizationRequiredException(
                    "Doctor token refresh failed; authorization must be completed again.",
                    exception);
            }

            tokenVault.Write(credential, refreshed.AccessToken, refreshed.RefreshToken, refreshed.ExpiresAt);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return refreshed.AccessToken;
        }
        finally
        {
            refreshCoordinator.Release(credential.Id);
        }
    }

    private static string Require(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException($"Tamin {name} is required.") : value;

    private static Uri RequireAbsoluteHttpsUri(string? value, string name) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) && uri.Scheme == Uri.UriSchemeHttps
            ? uri
            : throw new InvalidOperationException($"Tamin {name} must be an absolute HTTPS URI.");
}
