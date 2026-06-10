internal static class ModelMapperExtensions
{
    private static readonly ModelMapper ModelMapper = new();
    public static PatientReadDto ToPatientRead(this ProjectReadDto src) => ModelMapper.ToPatientRead(src);
    public static PatientWriteDto ToPatientWrite(this ProjectReadDto src) => ModelMapper.ToPatientWrite(src);
}
