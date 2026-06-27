using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Appointments.Domain.Dto;

public record ServiceDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    [JsonPropertyName("serviceTypeId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ServiceTypeId { get; set; }
}
