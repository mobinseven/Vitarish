namespace Vitarish.Files.Contracts.Requests;

public sealed record CompleteFileUploadRequest(Guid FileId, string Checksum);
