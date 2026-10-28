using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Security;

namespace Vitarish.Users.Infrastructure.Handlers;

internal sealed class GetUserPasswordStatusQueryHandler(UsersDbContext dbContext)
    : IRequestHandler<GetUserPasswordStatusQuery, UserPasswordStatus?>
{
    public async Task<UserPasswordStatus?> Handle(GetUserPasswordStatusQuery request, CancellationToken cancellationToken)
    {
        StoredCredential? credential = await dbContext.Users.AsNoTracking()
            .Where(user => user.Id == request.UserId)
            .Select(user => new StoredCredential(user.PasswordHash, user.PasswordHashFingerprint, user.PasswordChangedAt))
            .SingleOrDefaultAsync(cancellationToken);

        if (credential is null)
            return null;

        bool passwordSet = !string.IsNullOrWhiteSpace(credential.PasswordHash);
        byte[]? fingerprint = credential.PasswordHashFingerprint is null
            ? null
            : (byte[])credential.PasswordHashFingerprint.Clone();
        bool fingerprintMatches;
        try
        {
            fingerprintMatches = StoredPasswordHashFingerprint.Matches(credential.PasswordHash, fingerprint);
        }
        finally
        {
            if (fingerprint is not null)
                CryptographicOperations.ZeroMemory(fingerprint);
        }

        return new UserPasswordStatus(passwordSet, fingerprintMatches ? credential.PasswordChangedAt : null);
    }

    private sealed record StoredCredential(string? PasswordHash, byte[]? PasswordHashFingerprint, DateTime? PasswordChangedAt);
}
