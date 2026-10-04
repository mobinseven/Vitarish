using Microsoft.EntityFrameworkCore;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Authentication;

public sealed class WebAuthnCredentialLookup(UsersDbContext dbContext) : IWebAuthnCredentialLookup
{
    public async Task<IReadOnlyList<WebAuthnCredentialDescriptor>> GetAllowedCredentialsAsync(string userId, CancellationToken ct)
    {
        List<Domain.Entities.WebAuthnCredential> credentials = await dbContext.WebAuthnCredentials
            .Where(c => c.UserId == userId && c.IsActive)
            .ToListAsync(ct);

        return credentials.Select(c => new WebAuthnCredentialDescriptor(c.CredentialId, c.UserId)).ToList();
    }

    public async Task<WebAuthnCredentialData?> FindByCredentialIdAsync(byte[] credentialId, CancellationToken ct)
    {
        Domain.Entities.WebAuthnCredential? credential = await dbContext.WebAuthnCredentials
            .FirstOrDefaultAsync(c => c.CredentialId == credentialId, ct);

        if (credential is null || !credential.IsActive)
            return null;

        return new WebAuthnCredentialData(credential.UserId, credential.PublicKey, credential.SignatureCounter, credential.IsActive);
    }

    public async Task UpdateSignatureCounterAsync(byte[] credentialId, uint newCounter, CancellationToken ct)
    {
        Domain.Entities.WebAuthnCredential? credential = await dbContext.WebAuthnCredentials
            .FirstOrDefaultAsync(c => c.CredentialId == credentialId, ct);

        if (credential is not null)
        {
            credential.SignatureCounter = newCounter;
            credential.LastUsedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
