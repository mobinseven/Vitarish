namespace Vitarish.Files.Domain.Models;

/// <summary>
/// A Files-owned projection that callers can store instead of bytes or storage keys.
/// </summary>
public sealed record FileReference(
    Guid FileId,
    FileLifecycleState LifecycleState,
    FileIntegrityState IntegrityState,
    FileReferenceState ReferenceState);
