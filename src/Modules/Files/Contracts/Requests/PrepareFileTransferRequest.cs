using Vitarish.Files.Contracts.Metadata;

namespace Vitarish.Files.Contracts.Requests;

public sealed record PrepareFileTransferRequest(
    FileTransferPurposeDto Purpose,
    string ResourceType,
    string ResourceId);
