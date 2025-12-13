namespace Vitarish.Sea.Domain.Dto;

using System.Collections.Generic;

public sealed record EventSnapshot(
    Guid Id,
    string Name,
    bool Committed,
    IReadOnlyList<AttachmentSnapshot> Attachments);
