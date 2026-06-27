using Vitarish.Modules.Base.Interface;

namespace Vitarish.Sea.Interface.Components.Base;

public class SeaCommonBase : TenantCommonBase, IDisposable
{
    [Inject] protected ISeaClient SeaClient { get; set; } = default!;

    protected SeaError? LastSeaError { get; private set; }

    protected static T? ValueOrDefault<T>(SeaResult<T> result) => result.Succeeded ? result.Value : default;

    protected async Task<T?> ValueOrNotifyAsync<T>(Task<SeaResult<T>> resultTask)
    {
        SeaResult<T> result = await resultTask;
        if (result.Succeeded)
        {
            LastSeaError = null;
            return result.Value;
        }

        LastSeaError = result.Error;
        return default;
    }

    protected async Task<bool> CommandOrNotifyAsync(Task<SeaResult<SeaCommandResult>> resultTask)
    {
        SeaResult<SeaCommandResult> result = await resultTask;
        if (result.Succeeded && result.Value?.Succeeded is true)
        {
            LastSeaError = null;
            return true;
        }

        LastSeaError = result.Error ?? result.Value?.Error;
        return false;
    }
}
