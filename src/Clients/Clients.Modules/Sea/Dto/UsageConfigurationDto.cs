namespace Vitarish.Clients.Modules.Sea.Dto;

public sealed record UsageConfigurationDto : EntityDto
{
    [JsonConstructor]
    public UsageConfigurationDto(Guid id, string? reminderSignature, string metadata)
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

