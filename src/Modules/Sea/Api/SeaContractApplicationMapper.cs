using System.Text.Json;

namespace Vitarish.Sea.Api;

public static class SeaContractApplicationMapper
{
    public static T Convert<T>(object value)
    {
        if (value is T typedValue)
        {
            return typedValue;
        }

        // Manual mapping implementation for high-performance scenarios
        // This avoids reflection and serialization overhead
        return value switch
        {
            Domain.Dto.ProjectWriteDto p => (T)(object)new Contracts.Responses.ProjectWriteDto { Id = p.Id, Name = p.Name, Description = p.Description },
            Domain.Dto.ProcedureWriteDto p => (T)(object)new Contracts.Responses.ProcedureWriteDto { Id = p.Id, ProjectId = p.ProjectId, Name = p.Name },
            _ => throw new NotSupportedException($"Manual mapping for {value.GetType().Name} to {typeof(T).Name} is not implemented.")
        };
    }
}
