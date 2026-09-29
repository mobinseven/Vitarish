namespace Vitarish.Files.Domain.Models;

public sealed record FileUploadPreparation(
    Guid TransferId,
    FileTransferPolicyDefinition EffectivePolicy,
    DateTimeOffset ExpiresAt);
