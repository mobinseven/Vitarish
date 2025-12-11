using Microsoft.JSInterop;

namespace Accounts.Interface.Tests.TestInfrastructure;

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

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => identifier == "import" && typeof(TValue) == typeof(IJSObjectReference)
            ? new ValueTask<TValue>((TValue)module)
            : new ValueTask<TValue>(default(TValue)!);
}
