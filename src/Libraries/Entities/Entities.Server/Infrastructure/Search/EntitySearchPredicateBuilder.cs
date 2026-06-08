
using System.Linq.Expressions;

namespace Vitarish.Entities.Server.Infrastructure.Search;

public static class EntitySearchPredicateBuilder
{
    public static Expression<Func<T, bool>> BuildSearchPredicate<T>(
        IEnumerable<string> patterns,
        params Expression<Func<T, string>>[] selectors)
    {
        ParameterExpression parameter = Expression.Parameter(typeof(T), "e");

        Expression? body = null;
        MemberExpression efFunctions = Expression.Property(null, typeof(EF), nameof(EF.Functions));

        foreach (string pattern in patterns)
        {
            foreach (Expression<Func<T, string>> selector in selectors)
            {
                // e => EF.Functions.ILike(selector(e), pattern)
                InvocationExpression property = Expression.Invoke(selector, parameter);
                ConstantExpression patternConstant = Expression.Constant(pattern);

                MethodCallExpression ilikeCall = Expression.Call(
                    typeof(NpgsqlDbFunctionsExtensions),
                    nameof(NpgsqlDbFunctionsExtensions.ILike),
                    Type.EmptyTypes,
                    efFunctions,
                    property,
                    patternConstant
                );

                body = body == null
                    ? ilikeCall
                    : Expression.OrElse(body, ilikeCall);
            }
        }

        return Expression.Lambda<Func<T, bool>>(body ?? Expression.Constant(false), parameter);
    }
}
