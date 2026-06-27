using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Appointments.Contracts.Dto;

public record ShiftDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ShiftStatus? Status { get; set; }

    [JsonPropertyName("visibility")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ShiftVisibility? Visibility { get; set; }

    [JsonPropertyName("dayOfWeek")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DayOfWeek? DayOfWeek { get; set; }

    [JsonPropertyName("startTime")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TimeOnly? StartTime { get; set; }

    [JsonPropertyName("endTime")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TimeOnly? EndTime { get; set; }

    [JsonPropertyName("sortBy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ShiftSortBy? SortBy { get; set; }

    [JsonPropertyName("descending")]
    public bool Descending { get; set; } = true;

    [JsonPropertyName("serviceProviderId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ServiceProviderId { get; set; }

    [JsonPropertyName("serviceId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ServiceId { get; set; }
}
