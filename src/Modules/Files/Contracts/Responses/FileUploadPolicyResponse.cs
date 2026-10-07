namespace Vitarish.Files.Contracts.Responses;

/// <summary>
/// What a new upload must satisfy, so clients can check a file before sending it.
/// </summary>
public sealed record FileUploadPolicyResponse(long MaximumContentBytes, IReadOnlyList<FileUploadFormatResponse> Formats);

public sealed record FileUploadFormatResponse(string MediaType, IReadOnlyList<string> Extensions);
