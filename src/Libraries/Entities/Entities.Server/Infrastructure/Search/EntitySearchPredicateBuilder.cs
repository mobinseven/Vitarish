using System.Linq.Expressions;
using System.Reflection;

namespace Vitarish.Entities.Server.Infrastructure.Search;

public sealed class EntitySearchPredicateBuilder
{
    private static readonly MethodInfo StringReplaceMethod = typeof(string)
        .GetMethod(nameof(string.Replace), [typeof(string), typeof(string)])!;

    private static readonly MethodInfo StringToLowerMethod = typeof(string)
        .GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;

    private readonly EntitySearchTextNormalizer textNormalizer;

    public EntitySearchPredicateBuilder(EntitySearchTextNormalizer textNormalizer)
    {
        this.textNormalizer = textNormalizer ?? throw new ArgumentNullException(nameof(textNormalizer));
    }

    public Expression<Func<TEntity, bool>> BuildSearchPredicate<TEntity>(
        IEnumerable<string> patterns,
        params Expression<Func<TEntity, string?>>[] propertySelectors)
    {
        ArgumentNullException.ThrowIfNull(patterns);
        ArgumentNullException.ThrowIfNull(propertySelectors);

        using IEnumerator<string> enumerator = patterns.GetEnumerator();
        if (!enumerator.MoveNext() || propertySelectors.Length == 0)
        {
            return static _ => true;
        }

        ParameterExpression entity = Expression.Parameter(typeof(TEntity), "entity");
        Expression functions = Expression.Property(null, typeof(EF), nameof(EF.Functions));
        Expression escape = Expression.Constant("\\");

        Expression? predicate = null;

        do
        {
            Expression pattern = Expression.Constant(enumerator.Current);
            Expression? patternPredicate = null;

            foreach (Expression<Func<TEntity, string?>> selector in propertySelectors)
            {
                ReplaceParameterVisitor visitor = new(selector.Parameters[0], entity);
                Expression property = visitor.Visit(selector.Body)!;
                Expression normalized = NormalizeProperty(property);

                Expression like = Expression.Call(
                    typeof(DbFunctionsExtensions),
                    nameof(DbFunctionsExtensions.Like),
                    Type.EmptyTypes,
                    functions,
                    normalized,
                    pattern,
                    escape);

                patternPredicate = patternPredicate is null ? like : Expression.OrElse(patternPredicate, like);
            }

            if (patternPredicate is null)
            {
                continue;
            }

            predicate = predicate is null ? patternPredicate : Expression.OrElse(predicate, patternPredicate);
        }
        while (enumerator.MoveNext());

        return predicate is null
            ? static _ => true
            : Expression.Lambda<Func<TEntity, bool>>(predicate, entity);
    }

    private Expression NormalizeProperty(Expression property)
    {
        Expression normalized = property;

        foreach ((string variant, string replacement) in textNormalizer.Replacements)
        {
            normalized = Expression.Call(
                normalized,
                StringReplaceMethod,
                Expression.Constant(variant),
                Expression.Constant(replacement));
        }

        return Expression.Call(normalized, StringToLowerMethod);
    }

    private sealed class ReplaceParameterVisitor(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == source ? target : base.VisitParameter(node);
    }
}
