namespace Vitarish.Sea.Infrastructure.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
[RegisterTransient]
internal partial class ProceduresMapper
{
    public partial ProcedureDto ToDto(Procedure src);
    public partial Procedure ToDomain(ProcedureWriteDto src);
    public partial ProcedureDescriptor ToDomain(ProcedureDescriptorDto src);
}

