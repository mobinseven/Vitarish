namespace Vitarish.Sea.Domain.Dto;

public sealed record UsageConfigurationWriteDto : EntityWriteDto
{
    [JsonConstructor]
    public UsageConfigurationWriteDto(Guid id, string? reminderSignature, string metadata)
    {
        Id = id;
        ReminderSignature = reminderSignature;
        Metadata = metadata ?? string.Empty;
    }

    [JsonPropertyName("reminderSignature")]
    public string? ReminderSignature { get; set; }

    [JsonPropertyName("metadata")]
    public string Metadata { get; set; } = string.Empty;
}
