
namespace Vitarish.Sea.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for Procedure entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Sea.Domain.Dto.ProcedureReadDto ToDto(global::Vitarish.Sea.Domain.Entities.Procedure src);

    public partial global::Vitarish.Sea.Domain.Entities.Procedure ToDomain(global::Vitarish.Sea.Domain.Dto.ProcedureWriteDto src);

    public partial global::Vitarish.Sea.Domain.Dto.ProcedureWriteDto ToWrite(global::Vitarish.Sea.Domain.Dto.ProcedureReadDto src);

    public partial global::Vitarish.Sea.Domain.Models.ProcedureDescriptor ToDomain(global::Vitarish.Sea.Domain.Dto.ProcedureDescriptorDto src);
}
