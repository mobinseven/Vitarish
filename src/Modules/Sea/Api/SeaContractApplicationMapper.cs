namespace Vitarish.Sea.Api;

public static class SeaContractApplicationMapper
{
    private static readonly SeaContractDtoMapper Mapper = new();

    public static T Convert<T>(object value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value is T typedValue)
        {
            return typedValue;
        }

        object mappedValue = value switch
        {
            Domain.Dto.ProjectReadDto source when typeof(T) == typeof(Contracts.Responses.ProjectReadDto) => Mapper.ToContract(source),
            Domain.Dto.ProjectWriteDto source when typeof(T) == typeof(Contracts.Responses.ProjectWriteDto) => Mapper.ToContract(source),
            Domain.Dto.ProjectSnapshot source when typeof(T) == typeof(Contracts.Responses.ProjectSnapshot) => Mapper.ToContract(source),
            Domain.Dto.ProcedureReadDto source when typeof(T) == typeof(Contracts.Responses.ProcedureReadDto) => Mapper.ToContract(source),
            Domain.Dto.ProcedureWriteDto source when typeof(T) == typeof(Contracts.Responses.ProcedureWriteDto) => Mapper.ToContract(source),
            Domain.Dto.EventReadDto source when typeof(T) == typeof(Contracts.Responses.EventReadDto) => Mapper.ToContract(source),
            Domain.Dto.AttachmentReadDto source when typeof(T) == typeof(Contracts.Responses.AttachmentReadDto) => Mapper.ToContract(source),
            Domain.Dto.RoleReadDto source when typeof(T) == typeof(Contracts.Responses.RoleReadDto) => Mapper.ToContract(source),
            Domain.Dto.UserRoleReadDto source when typeof(T) == typeof(Contracts.Responses.UserRoleReadDto) => Mapper.ToContract(source),
            Domain.Dto.DatabaseStatisticsReadDto source when typeof(T) == typeof(Contracts.Responses.DatabaseStatisticsReadDto) => Mapper.ToContract(source),
            Contracts.Responses.ProjectWriteDto source when typeof(T) == typeof(Domain.Dto.ProjectWriteDto) => Mapper.ToDomain(source),
            Contracts.Responses.ProcedureWriteDto source when typeof(T) == typeof(Domain.Dto.ProcedureWriteDto) => Mapper.ToDomain(source),
            _ => throw new NotSupportedException($"Mapping for {value.GetType().Name} to {typeof(T).Name} is not implemented.")
        };

        return (T)mappedValue;
    }
}
