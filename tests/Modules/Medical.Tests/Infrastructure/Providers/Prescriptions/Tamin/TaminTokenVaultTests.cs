using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Vitarish.Medical.Domain.Entities.Prescriptions;
using Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

namespace Medical.Tests.Infrastructure.Providers.Prescriptions.Tamin;

public sealed class TaminTokenVaultTests
{
    private readonly TaminTokenVault _vault = new(new EphemeralDataProtectionProvider());

    [Fact]
    public void Write_and_read_round_trip_access_and_refresh_tokens()
    {
        DoctorProviderCredential credential = CreateCredential();
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        _vault.Write(credential, "access-token", "refresh-token", expiresAt);

        credential.TokenSecretRef.Should().NotBe("access-token");
        credential.RefreshTokenSecretRef.Should().NotBe("refresh-token");
        TaminStoredToken stored = _vault.Read(credential);
        stored.AccessToken.Should().Be("access-token");
        stored.RefreshToken.Should().Be("refresh-token");
        stored.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void Read_accepts_legacy_plaintext_access_token_during_compatibility_window()
    {
        DoctorProviderCredential credential = CreateCredential();
        credential.TokenSecretRef = "legacy-access-token";

        TaminStoredToken stored = _vault.Read(credential);

        stored.AccessToken.Should().Be("legacy-access-token");
    }

    [Fact]
    public void Authorization_state_is_protected_and_round_trips()
    {
        DoctorProviderCredential credential = CreateCredential();
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddMinutes(10);

        _vault.WriteAuthorizationState(credential, "state", "verifier", expiresAt);

        credential.SessionId.Should().NotContain("verifier");
        TaminAuthorizationState? stored = _vault.ReadAuthorizationState(credential);
        stored.Should().NotBeNull();
        stored!.State.Should().Be("state");
        stored.CodeVerifier.Should().Be("verifier");
        stored.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromMilliseconds(1));
    }

    private static DoctorProviderCredential CreateCredential() => new()
    {
        Id = Guid.NewGuid(),
        DoctorId = Guid.NewGuid(),
        ClinicId = Guid.NewGuid(),
        ProviderKey = TaminInsuranceAdapter.ProviderKeyConst
    };
}
