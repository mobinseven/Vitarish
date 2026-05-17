namespace Vitarish.Links.Domain.Dto;

public record LinkReadDto : EntityReadDto
{
    [JsonPropertyName("tenantId")]
    public Guid TenantId { get; set; }

    [JsonPropertyName("domain")]
    public string Domain { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("destinationUrl")]
    public string DestinationUrl { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public LinkStatus Status { get; set; }

    [JsonPropertyName("expiresAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? ExpiresAt { get; set; }
}
