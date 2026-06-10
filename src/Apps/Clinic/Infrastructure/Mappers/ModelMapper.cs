using Riok.Mapperly.Abstractions;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clinic.Infrastructure.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
internal partial class ModelMapper
{
    public partial PatientReadDto ToPatientRead(ProjectReadDto src);
    public partial PatientWriteDto ToPatientWrite(ProjectReadDto src);
}
