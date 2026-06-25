namespace Vitarish.Appointments.Domain.Dto;

public record ShiftReminderPolicyDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("shiftId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ShiftId { get; set; }

    [JsonPropertyName("isEnabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsEnabled { get; set; }

    [JsonPropertyName("channel")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ShiftReminderChannel? Channel { get; set; }
}
