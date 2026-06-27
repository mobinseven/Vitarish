using System.Text.Json;

namespace Vitarish.Sea.Api;

public static class SeaContractApplicationMapper
{
using System;

namespace Vitarish.Sea.Api;

public static class SeaContractApplicationMapper
{
    public static T Convert<T>(object value)
    {
        if (value is T typedValue) return typedValue;

        // Manual mapping implementation for high-performance scenarios
        // This avoids reflection and serialization overhead
        return value switch
        {
            Vitarish.Sea.Domain.Dto.ProjectWriteDto p => (T)(object)new Vitarish.Sea.Contracts.Responses.ProjectWriteDto { Id = p.Id, Title = p.Title, Description = p.Description },
            Vitarish.Sea.Domain.Dto.ProcedureWriteDto p => (T)(object)new Vitarish.Sea.Contracts.Responses.ProcedureWriteDto { Id = p.Id, ProjectId = p.ProjectId, Name = p.Name },
            _ => throw new NotSupportedException($"Manual mapping for {value.GetType().Name} to {typeof(T).Name} is not implemented.")
        };
    }
}
}
