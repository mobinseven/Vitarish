using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Commerce.Interface.Components.Base;

public static class CommerceResultExtensions
{
    public static async Task<T?> GetValueOrDefaultAsync<T>(this Task<EntityResult<T>> resultTask)
    {
        EntityResult<T>? result = await resultTask.ConfigureAwait(false);
        return result is { IsSuccess: true, Value: var value } ? value : default;
    }
}
