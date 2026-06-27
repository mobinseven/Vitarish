using Microsoft.AspNetCore.Components;
using Vitarish.Commerce.Contracts;

namespace Vitarish.Commerce.Interface.Components.Base;

public abstract class CommerceComponentBase : ComponentBase, IDisposable
{
    private readonly CancellationTokenSource componentCancellation = new();
    private bool disposed;

    [Inject]
    protected ICommerceClient CommerceClient { get; set; } = default!;

    protected CancellationToken ComponentCancellationToken => componentCancellation.Token;

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            componentCancellation.Cancel();
            componentCancellation.Dispose();
        }

        disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
