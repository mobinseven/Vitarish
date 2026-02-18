namespace Vitarish.Sea.Domain.Dto;

public record ProcedureWriteDto : EntityWriteDto
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; }
    public string? Description
    {
        get => MetadataValues.GetField<string>(Metadata);
        set
        {
            MetadataValues.SetField(value);
            Metadata = MetadataValues.ToDictionary();
        }
    }
}
