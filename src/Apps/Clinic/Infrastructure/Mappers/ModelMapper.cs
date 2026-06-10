using Riok.Mapperly.Abstractions;

namespace Vitarish.Clinic.Infrastructure.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
internal partial class ModelMapper
{
    public partial PatientReadDto ToPatientRead(ProjectReadDto src);
    public partial PatientWriteDto ToPatientWrite(ProjectReadDto src);
}
