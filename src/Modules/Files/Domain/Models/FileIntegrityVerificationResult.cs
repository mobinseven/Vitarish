namespace Vitarish.Files.Domain.Models;

public sealed record FileIntegrityVerificationResult(
    FileIntegrityState State,
    string? ObjectKey,
    long ExpectedLength,
    string? ExpectedContentType)
{
    public bool IsVerified => State is FileIntegrityState.Verified;
}
