using Riok.Mapperly.Abstractions;
using Vitarish.Clinic.Interface.Models;

namespace Vitarish.Clinic.Interface.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
internal partial class ModelMapper
{
    public partial PatientReadDto ToPatientRead(ProjectReadDto src);
    public partial PatientWriteDto ToPatientWrite(ProjectReadDto src);
}
