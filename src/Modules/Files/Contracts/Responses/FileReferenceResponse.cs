using Vitarish.Files.Contracts.Metadata;

namespace Vitarish.Files.Contracts.Responses;

public sealed record FileReferenceResponse(
    Guid FileId,
    FileLifecycleStateDto LifecycleState,
    FileIntegrityStateDto IntegrityState,
    FileReferenceStateDto ReferenceState);
