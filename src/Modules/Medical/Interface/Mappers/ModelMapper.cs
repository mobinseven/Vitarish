using Riok.Mapperly.Abstractions;
using Vitarish.Medical.Interface.Models;

namespace Vitarish.Medical.Interface.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
internal partial class ModelMapper
{
    public partial PatientReadDto ToPatientRead(ProjectReadDto src);
    public partial PatientWriteDto ToPatientWrite(ProjectReadDto src);
}
