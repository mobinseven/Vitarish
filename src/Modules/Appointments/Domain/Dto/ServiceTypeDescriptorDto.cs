namespace Vitarish.Appointments.Domain.Dto;

public record ServiceTypeDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }
}
