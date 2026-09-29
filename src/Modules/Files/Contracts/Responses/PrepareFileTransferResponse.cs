namespace Vitarish.Files.Contracts.Responses;

public sealed record PrepareFileTransferResponse(
    Guid TransferId,
    FileTransferPolicyResponse EffectivePolicy,
    DateTimeOffset ExpiresAt);
