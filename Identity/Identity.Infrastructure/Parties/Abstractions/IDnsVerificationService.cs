namespace Vitarish.Identity.Infrastructure.Parties.Abstractions;

/// <summary>
/// DNS-based verification helper.
/// </summary>
public interface IDnsVerificationService
{
    Task<bool> VerifyTxtRecordAsync(string hostname, string expectedHash, CancellationToken cancellationToken = default);
}
