namespace Vitarish.Sea.Infrastructure.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
[RegisterTransient]
internal partial class ProceduresMapper
{
    public partial ProcedureDto ToDto(Procedure src);
    public partial Procedure ToDomain(ProcedureDto src);
    public partial ProcedureDescriptor ToDomain(ProcedureDescriptorDto src);
}

