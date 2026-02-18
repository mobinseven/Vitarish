namespace Vitarish.Sea.Domain.Dto;

public sealed record EventSnapshot(
    Guid Id,
    string Name,
    bool Committed,
    IReadOnlyList<AttachmentSnapshot> Attachments);
