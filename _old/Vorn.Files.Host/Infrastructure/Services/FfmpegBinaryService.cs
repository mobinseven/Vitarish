using FFMpegCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vorn.Files.Host.Domain.Options;

namespace Vorn.Files.Host.Infrastructure.Services;

public class FfmpegBinaryService
{
    private readonly ILogger<FfmpegBinaryService> _logger;
    private readonly string _binaryFolder;

    public FfmpegBinaryService(VornOptions options, ILogger<FfmpegBinaryService> logger)
    {
        _logger = logger;
        _binaryFolder = Path.GetFullPath(options.Files.FfmpegBinaryFolder);
        Directory.CreateDirectory(_binaryFolder);
        GlobalFFOptions.Configure(o => o.BinaryFolder = _binaryFolder);
    }

    public async Task EnsureExistsAsync(CancellationToken token = default)
    {
        string ffmpegPath = GlobalFFOptions.GetFFMpegBinaryPath();
        string ffprobePath = GlobalFFOptions.GetFFProbeBinaryPath();
        if (File.Exists(ffmpegPath) && File.Exists(ffprobePath))
        {
            return;
        }

        _logger.LogInformation("Downloading FFMpeg binaries to {Folder}", _binaryFolder);
        await DownloadBinariesAsync(token);
    }

    private async Task DownloadBinariesAsync(CancellationToken token)
    {
        using HttpClient client = new();
        string metaJson = await client.GetStringAsync("https://ffbinaries.com/api/v1/version/latest", token);
        using JsonDocument doc = JsonDocument.Parse(metaJson);
        string platform = GetPlatform();
        JsonElement bin = doc.RootElement.GetProperty("bin").GetProperty(platform);
        await DownloadAndExtractAsync(client, bin.GetProperty("ffmpeg").GetString()!, token);
        await DownloadAndExtractAsync(client, bin.GetProperty("ffprobe").GetString()!, token);
    }

    private async Task DownloadAndExtractAsync(HttpClient client, string url, CancellationToken token)
    {
        string temp = Path.GetTempFileName();
        await using (Stream stream = await client.GetStreamAsync(url, token))
        await using (FileStream fs = File.Create(temp))
        {
            await stream.CopyToAsync(fs, token);
        }
        ZipFile.ExtractToDirectory(temp, _binaryFolder, true);
        File.Delete(temp);
    }

    private static string GetPlatform()
    {
        bool is64 = Environment.Is64BitProcess;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return $"windows-{(is64 ? "64" : "32")}";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "osx-64";
        }

        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            return "linux-arm64";
        }

        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm)
        {
            return "linux-armhf";
        }

        return $"linux-{(is64 ? "64" : "32")}";
    }
}
