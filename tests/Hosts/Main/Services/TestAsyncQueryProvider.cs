using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Vitarish.Main.Mock.Services;

internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression) =>
        new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
        new TestAsyncEnumerable<TElement>(expression);

    public object? Execute(Expression expression) => _inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression) =>
        _inner.Execute<TResult>(expression);

    public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression) =>
        new TestAsyncEnumerable<TResult>(expression);

    public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken) =>
        Task.FromResult(Execute<TResult>(expression));
    TResult IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken) => ExecuteAsync<TResult>(expression, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
}
