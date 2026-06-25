namespace Vitarish.Files.Contracts.Responses;

public sealed record FileDownloadDescriptorResponse(Guid FileId, string ContentType, long Size, string? FileName);
