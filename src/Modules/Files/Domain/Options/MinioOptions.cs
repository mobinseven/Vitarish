using Microsoft.Extensions.Configuration;
using System.Data.Common;

namespace Vitarish.Files.Domain.Options;

public class MinioOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public int Port { get; set; } = 443;
    public bool UseSsl { get; set; } = true;
    public bool ForcePathStyle { get; set; } = true;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string? Region { get; set; }
    public int HttpRequestTimeoutSeconds { get; set; } = 100;

    public MinioOptions()
    {
    }

    public MinioOptions(IConfiguration configuration, string name = "Minio")
    {
        MinioOptions opts = Load(configuration, name);
        Endpoint = opts.Endpoint;
        Port = opts.Port;
        UseSsl = opts.UseSsl;
        ForcePathStyle = opts.ForcePathStyle;
        AccessKey = opts.AccessKey;
        SecretKey = opts.SecretKey;
        BucketName = opts.BucketName;
        Region = opts.Region;
        HttpRequestTimeoutSeconds = opts.HttpRequestTimeoutSeconds;
    }

    public static MinioOptions Load(IConfiguration configuration, string name = "Minio")
    {
        MinioOptions opts = new();
        string? cs = configuration.GetConnectionString(name);
        if (string.IsNullOrEmpty(cs))
        {
            throw new InvalidOperationException($"Connection string '{name}' not found.");
        }

        DbConnectionStringBuilder csb = new()
        {
            ConnectionString = cs
        };

        if (csb.TryGetValue("Endpoint", out object? value) && value != null)
        {
            opts.Endpoint = value.ToString()!;
        }

        if (csb.TryGetValue("Port", out value) && int.TryParse(value?.ToString(), out int port))
        {
            opts.Port = port;
        }

        if (csb.TryGetValue("UseSsl", out value) && bool.TryParse(value?.ToString(), out bool ssl))
        {
            opts.UseSsl = ssl;
        }

        if (csb.TryGetValue("ForcePathStyle", out value) && bool.TryParse(value?.ToString(), out bool forcePathStyle))
        {
            opts.ForcePathStyle = forcePathStyle;
        }

        if (csb.TryGetValue("AccessKey", out value) && value != null)
        {
            opts.AccessKey = value.ToString()!;
        }

        if (csb.TryGetValue("SecretKey", out value) && value != null)
        {
            opts.SecretKey = value.ToString()!;
        }

        if (csb.TryGetValue("BucketName", out value) && value != null)
        {
            opts.BucketName = value.ToString()!;
        }

        if (csb.TryGetValue("Region", out value))
        {
            opts.Region = value?.ToString();
        }

        if (csb.TryGetValue("HttpRequestTimeoutSeconds", out value)
            && int.TryParse(value?.ToString(), out int timeoutSeconds)
            && timeoutSeconds > 0)
        {
            opts.HttpRequestTimeoutSeconds = timeoutSeconds;
        }
        return opts;
    }

    public string BuildServiceUrl()
    {
        string scheme = UseSsl ? "https" : "http";
        return $"{scheme}://{Endpoint}:{Port}";
    }
}
