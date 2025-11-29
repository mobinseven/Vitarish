using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Vorn.Aaas.Interface.Tests.TestInfrastructure;

internal sealed class TestJsRuntime : IJSRuntime
{
    private sealed class TestJsModule : IJSObjectReference
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => new(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => new(default(TValue)!);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private readonly IJSObjectReference module = new TestJsModule();

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if(identifier == "import" && typeof(TValue) == typeof(IJSObjectReference))
        {
            return new ValueTask<TValue>((TValue)module);
        }

        return new ValueTask<TValue>(default(TValue)!);
    }
}
