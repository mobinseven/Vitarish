using Vitarish.Files.Contracts.Metadata;

namespace Vitarish.Files.Contracts.Responses;

public sealed record FileTransferPolicyResponse(
    FileTransferPurposeDto Purpose,
    long MaximumContentBytes,
    IReadOnlyList<string> AllowedContentTypes);
