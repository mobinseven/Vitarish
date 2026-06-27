using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Appointments.Client;

public static class EntityResultClientExtensions
{
    public static T? GetValueOrDefault<T>(this EntityResult<T> result) => result.IsSuccess ? result.Value : default;

    public static async Task<T?> GetValueOrDefaultAsync<T>(this Task<EntityResult<T>> resultTask)
    {
        EntityResult<T> result = await resultTask.ConfigureAwait(false);
        return result.GetValueOrDefault();
    }
}
