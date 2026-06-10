using Vitarish.Clinic.Domain.Models;
using Vitarish.Clinic.Infrastructure.Mappers;
using Vitarish.Sea.Domain.Dto;

public static class ModelMapperExtensions
{
    private static readonly ModelMapper ModelMapper = new();
    public static PatientReadDto ToPatientRead(this ProjectReadDto src) => ModelMapper.ToPatientRead(src);
    public static PatientWriteDto ToPatientWrite(this ProjectReadDto src) => ModelMapper.ToPatientWrite(src);
}
