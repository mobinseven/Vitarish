namespace Vorn.Files.Host.Domain.Options;

public class FilesOptions
{
    public string Prefix { get; set; }
    public string BaseUrl { get; set; }
    public string UploadPath { get; set; }
    public string FfmpegBinaryFolder { get; set; } = "FFmpeg";
}
