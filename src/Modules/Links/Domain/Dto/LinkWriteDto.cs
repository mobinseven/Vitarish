using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Links.Domain.Dto;

public record LinkWriteDto : EntityWriteDto
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
    public LinkStatus Status { get; set; } = LinkStatus.Active;

    [JsonPropertyName("expiresAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? ExpiresAt { get; set; }
}
