namespace Vitarish.Clients.Modules.Files.Models;

public sealed record PendingFileDto(string Name, long Size, string Type, string Checksum, string Thumbnail)
{
    public double Progress { get; set; }
    public string? UserId { get; set; }
}
