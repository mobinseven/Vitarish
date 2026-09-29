namespace Vitarish.Files.Domain.Models;

public sealed record FileTransferPolicyDefinition(
    FileTransferPurpose Purpose,
    long MaximumContentBytes,
    IReadOnlySet<string> AllowedContentTypes);
