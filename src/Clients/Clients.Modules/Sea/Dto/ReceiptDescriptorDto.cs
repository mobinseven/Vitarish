namespace Vitarish.Clients.Modules.Sea.Dto;

public record ReceiptDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("projectId")]
    public Guid? ProjectId { get; init; }

    [JsonPropertyName("procedureId")]
    public Guid? ProcedureId { get; init; }

    [JsonPropertyName("eventId")]
    public Guid? EventId { get; init; }

    [JsonPropertyName("objectId")]
    public Guid? ObjectId { get; init; }
}
