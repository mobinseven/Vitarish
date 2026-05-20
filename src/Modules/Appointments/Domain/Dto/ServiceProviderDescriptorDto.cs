namespace Vitarish.Appointments.Domain.Dto;

public record ServiceProviderDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }
}
