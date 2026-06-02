using Vitarish.Users.Domain.Entities;

namespace Users.Tests.Domain;

public class WebAuthnCredentialTests
{
    [Fact]
    public void WebAuthnCredential_DefaultsToActiveAndNoLastUsed()
    {
        WebAuthnCredential credential = new()
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            CredentialId = [1, 2, 3],
            PublicKey = [4, 5, 6],
            CredentialType = "public-key",
            RegisteredAt = DateTimeOffset.UtcNow
        };

        _ = credential.IsActive.Should().BeTrue();
        _ = credential.LastUsedAt.Should().BeNull();
    }

    [Fact]
    public void WebAuthnCredential_CanUpdateLastUsedAndDeactivate()
    {
        WebAuthnCredential credential = new()
        {
            Id = Guid.NewGuid(),
            UserId = "user-2",
            CredentialId = [7, 8, 9],
            PublicKey = [10, 11, 12],
            CredentialType = "public-key",
            RegisteredAt = DateTimeOffset.UtcNow
        };
        DateTimeOffset lastUsed = DateTimeOffset.UtcNow.AddMinutes(-5);

        credential.LastUsedAt = lastUsed;
        credential.IsActive = false;

        _ = credential.LastUsedAt.Should().Be(lastUsed);
        _ = credential.IsActive.Should().BeFalse();
    }
}
