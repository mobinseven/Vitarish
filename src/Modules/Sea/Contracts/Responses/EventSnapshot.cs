namespace Vitarish.Sea.Contracts.Responses;

public sealed record EventSnapshot(
    Guid Id,
    string Name,
    bool Committed,
    IReadOnlyList<AttachmentSnapshot> Attachments);
