using Riok.Mapperly.Abstractions;
using Vitarish.Clinical.Interface.Models;

namespace Vitarish.Clinical.Interface.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
internal partial class ModelMapper
{
    public partial PatientReadDto ToPatientRead(ProjectReadDto src);
    public partial PatientWriteDto ToPatientWrite(ProjectReadDto src);
}
