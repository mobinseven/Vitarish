using System.ComponentModel.DataAnnotations;

namespace Vitarish.Modules.Elasticsearch.Infrastructure.Configuration;

public class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    [Required]
    public required Uri Uri { get; init; }

    public ElasticsearchAuthOptions Auth { get; init; } = new();

    [Required]
    [MinLength(1)]
    public string IndexPrefix { get; init; } = "vitarish";

    [Range(1, 100)]
    public int DefaultShards { get; init; } = 1;

    [Range(0, 10)]
    public int DefaultReplicas { get; init; } = 1;

    [Range(typeof(TimeSpan), "00:00:01", "00:10:00")]
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);

    [Range(typeof(TimeSpan), "00:00:01", "00:05:00")]
    public TimeSpan PingTimeout { get; init; } = TimeSpan.FromSeconds(5);

    [Range(typeof(TimeSpan), "00:00:01", "00:10:00")]
    public TimeSpan MaxRetryTimeout { get; init; } = TimeSpan.FromSeconds(30);

    [Range(0, 10)]
    public int MaxRetries { get; init; } = 2;


    public ElasticsearchIndexingProcessorOptions IndexingProcessor { get; init; } = new();
}

public class ElasticsearchIndexingProcessorOptions
{
    [Range(1, 20)]
    public int MaxAttempts { get; init; } = 5;

    [Range(typeof(TimeSpan), "00:00:00.100", "00:05:00")]
    public TimeSpan BaseBackoff { get; init; } = TimeSpan.FromSeconds(1);

    [Range(typeof(TimeSpan), "00:00:00.100", "00:15:00")]
    public TimeSpan MaxBackoff { get; init; } = TimeSpan.FromSeconds(30);
}


public class ElasticsearchAuthOptions
{
    public string? Username { get; init; }

    public string? Password { get; init; }

    public string? ApiKey { get; init; }
}
