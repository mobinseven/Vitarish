using System.Data.Common;

namespace Vorn.Files.Host.Domain.Options;

public class MinioOptions
{
    public string Endpoint { get; set; }
    public int Port { get; set; } = 443;
    public bool UseSsl { get; set; } = true;
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string BucketName { get; set; }
    public string? Region { get; set; }
    public string? ObjectPrefix { get; set; }

    public MinioOptions(IConfiguration configuration, string name = "Minio")
    {
        var cs = configuration.GetConnectionString(name);
        if(string.IsNullOrEmpty(cs))
            throw new InvalidOperationException($"Connection string '{name}' not found.");

        var csb = new DbConnectionStringBuilder
        {
            ConnectionString = cs
        };

        object value;

        if(csb.TryGetValue("Endpoint", out value))
            Endpoint = value.ToString()!;

        if(csb.TryGetValue("Port", out value) && int.TryParse(value.ToString(), out var port))
            Port = port;

        if(csb.TryGetValue("UseSsl", out value) && bool.TryParse(value.ToString(), out var ssl))
            UseSsl = ssl;

        if(csb.TryGetValue("AccessKey", out value))
            AccessKey = value.ToString()!;

        if(csb.TryGetValue("SecretKey", out value))
            SecretKey = value.ToString()!;

        if(csb.TryGetValue("BucketName", out value))
            BucketName = value.ToString()!;

        if(csb.TryGetValue("Region", out value))
            Region = value.ToString();

        if(csb.TryGetValue("ObjectPrefix", out value))
            ObjectPrefix = value.ToString();
    }
}
