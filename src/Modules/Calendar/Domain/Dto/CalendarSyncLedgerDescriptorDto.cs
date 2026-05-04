namespace Vitarish.Calendar.Domain.Dto;

public record CalendarSyncLedgerDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("calendarAccountId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? CalendarAccountId { get; set; }

    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SyncStatus? Status { get; set; }
}
