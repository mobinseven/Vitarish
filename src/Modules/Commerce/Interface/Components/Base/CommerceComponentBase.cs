using Microsoft.AspNetCore.Components;
using Vitarish.Commerce.Contracts;

namespace Vitarish.Commerce.Interface.Components.Base;

public abstract class CommerceComponentBase : ComponentBase, IDisposable
{
    private readonly CancellationTokenSource componentCancellation = new();

    [Inject]
    protected ICommerceClient CommerceClient { get; set; } = default!;

    protected CancellationToken ComponentCancellationToken => componentCancellation.Token;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        componentCancellation.Cancel();
        componentCancellation.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
