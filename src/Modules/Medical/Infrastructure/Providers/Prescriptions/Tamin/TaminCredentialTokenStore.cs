using Tamin.Integration.Auth;
using Vitarish.Medical.Domain.Entities.Prescriptions;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

/// <summary>
/// Keeps one doctor credential's Tamin token in the encrypted credential row, so every request and instance
/// sees the same token and only one refresh runs at a time per credential.
/// </summary>
internal sealed class TaminCredentialTokenStore(
    DoctorProviderCredential credential,
    PrescriptionDbContext dbContext,
    TaminTokenVault tokenVault,
    TaminCredentialRefreshCoordinator refreshCoordinator) : ITaminTokenStore
{
    public async Task<IAsyncDisposable> AcquireAsync(CancellationToken cancellationToken)
    {
        await refreshCoordinator.WaitAsync(credential.Id, cancellationToken);
        return new Lease(refreshCoordinator, credential.Id);
    }

    public async Task<DoctorToken?> ReadAsync(CancellationToken cancellationToken)
    {
        await dbContext.Entry(credential).ReloadAsync(cancellationToken);
        TaminStoredToken stored = tokenVault.Read(credential);
        return string.IsNullOrWhiteSpace(stored.AccessToken) && string.IsNullOrWhiteSpace(stored.RefreshToken)
            ? null
            : new DoctorToken(stored.AccessToken ?? string.Empty, stored.RefreshToken, stored.ExpiresAt);
    }

    public async Task WriteAsync(DoctorToken? token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            tokenVault.Clear(credential);
        }
        else
        {
            // The vault keeps the stored refresh token when a refresh response carries none.
            tokenVault.Write(credential, token.AccessToken, token.RefreshToken, token.ExpiresAt);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed class Lease(TaminCredentialRefreshCoordinator coordinator, Guid credentialId) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            coordinator.Release(credentialId);
            return ValueTask.CompletedTask;
        }
    }
}
