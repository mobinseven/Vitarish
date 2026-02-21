using Riok.Mapperly.Abstractions;
using Vitarish.Sea.Interface.Plugins.Clinic.Models;

namespace Vitarish.Sea.Interface.Plugins.Clinic.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
internal partial class ModelMapper
{
    public partial ClinicProjectReadDto ToClinicRead(ProjectReadDto src);
    public partial ClinicProjectWriteDto ToClinicWrite(ProjectReadDto src);
}
