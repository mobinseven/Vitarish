using Microsoft.AspNetCore.Http;

namespace Vitarish.Files.Api;

/// <summary>
/// The OpenAPI shape of the multipart form the upload endpoint reads. Its fields mirror
/// <see cref="Contracts.Requests.FileUploadRequest"/> plus the file content; the handler reads the form
/// itself so it can reject oversized requests before buffering them.
/// </summary>
public sealed record FileUploadForm
{
    public required IFormFile Content { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public long Size { get; init; }
    public required string Checksum { get; init; }
    public string? Thumbnail { get; init; }
    public string? Tag { get; init; }
}
