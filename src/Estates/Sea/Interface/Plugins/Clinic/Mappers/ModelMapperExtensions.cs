using Vitarish.Sea.Interface.Plugins.Clinic.Mappers;
using Vitarish.Sea.Interface.Plugins.Clinic.Models;

internal static class ModelMapperExtensions
{
    private static readonly ModelMapper ModelMapper = new();
    public static ClinicProjectReadDto ToClinicRead(this ProjectReadDto src) => ModelMapper.ToClinicRead(src);
    public static ClinicProjectWriteDto ToClinicWrite(this ProjectReadDto src) => ModelMapper.ToClinicWrite(src);
}
