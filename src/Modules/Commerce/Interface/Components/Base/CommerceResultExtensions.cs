using Vitarish.Entities.Contracts;

namespace Vitarish.Commerce.Interface.Components.Base;

public static class CommerceResultExtensions
{
    public static async Task<T?> GetValueOrDefaultAsync<T>(this Task<EntityResult<T>> resultTask)
    {
        EntityResult<T> result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? result.Value : default;
    }
}
