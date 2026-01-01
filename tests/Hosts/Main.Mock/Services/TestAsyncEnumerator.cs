namespace Vitarish.Main.Mock.Services;

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return default;
    }

    public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());

    public T Current => _inner.Current;
}
