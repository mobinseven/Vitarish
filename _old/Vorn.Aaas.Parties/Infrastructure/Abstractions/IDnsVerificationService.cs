namespace Vorn.Aaas.Parties.Infrastructure.Abstractions;

/// <summary>
/// Service for DNS-based domain verification.
/// </summary>
public interface IDnsVerificationService
{
    /// <summary>
    /// Verifies a DNS TXT record at the specified hostname contains the expected value.
    /// </summary>
    /// <param name="hostname">The hostname to query (e.g., _verify.example.com).</param>
    /// <param name="expectedHash">The expected SHA256 hash value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if verification succeeds, false otherwise.</returns>
    Task<bool> VerifyTxtRecordAsync(string hostname, string expectedHash, CancellationToken cancellationToken = default);
}
