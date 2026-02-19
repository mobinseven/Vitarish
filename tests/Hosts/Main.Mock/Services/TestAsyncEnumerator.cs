namespace Vitarish.Main.Mock.Services;

internal class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public ValueTask DisposeAsync()
    {
        inner.Dispose();
        return default;
    }

    public ValueTask<bool> MoveNextAsync() => new(inner.MoveNext());

    public T Current => inner.Current;
}
