using System.Linq.Expressions;
using System.Reflection;

namespace Vitarish.Entities.Server.Infrastructure;

public static class QueryExtensions
{
    public static IQueryable<T> ApplyDescription<T>(this IQueryable<T> query, EntityDescriptor descriptor) where T : Entity
    {
        if(descriptor is { DeletedFrom: not null } or { DeletedTo: not null } or { DeletedBy: not null })
        {
            query = query.IgnoreQueryFilters();
        }

        if(descriptor.CreatedFrom is not null)
        {
            query = query.Where(e => e.CreatedAt >= descriptor.CreatedFrom);
        }

        if(descriptor.CreatedTo is not null)
        {
            query = query.Where(e => e.CreatedAt <= descriptor.CreatedTo);
        }

        if(!string.IsNullOrWhiteSpace(descriptor.CreatedBy))
        {
            query = query.Where(e => e.CreatedBy == descriptor.CreatedBy);
        }

        if(descriptor.UpdatedFrom is not null)
        {
            query = query.Where(e => e.UpdatedAt >= descriptor.UpdatedFrom);
        }

        if(descriptor.UpdatedTo is not null)
        {
            query = query.Where(e => e.UpdatedAt <= descriptor.UpdatedTo);
        }

        if(!string.IsNullOrWhiteSpace(descriptor.UpdatedBy))
        {
            query = query.Where(e => e.UpdatedBy == descriptor.UpdatedBy);
        }

        if(descriptor.DeletedFrom is not null)
        {
            query = query.Where(e => e.DeletedAt >= descriptor.DeletedFrom);
        }

        if(descriptor.DeletedTo is not null)
        {
            query = query.Where(e => e.DeletedAt <= descriptor.DeletedTo);
        }

        if(!string.IsNullOrWhiteSpace(descriptor.DeletedBy))
        {
            query = query.Where(e => e.DeletedBy == descriptor.DeletedBy);
        }

        return query;
    }

    public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> query, EntityDescriptor descriptor) where T : Entity
    {
        if(descriptor.Skip is null && descriptor.Take is null)
        {
            return query;
        }

        if(!HasOrdering(query.Expression))
        {
            query = query.OrderByDescending(e => e.UpdatedAt ?? e.CreatedAt);
        }

        if(descriptor.Skip is not null)
        {
            query = query.Skip(descriptor.Skip.Value);
        }

        if(descriptor.Take > 0)
        {
            query = query.Take(descriptor.Take.Value);
        }

        return query;
    }

    private static bool HasOrdering(Expression expression)
    {
        if(expression is MethodCallExpression methodCall)
        {
            if(IsOrderingMethod(methodCall.Method))
            {
                return true;
            }

            if(methodCall.Arguments.Count > 0)
            {
                return HasOrdering(Unwrap(methodCall.Arguments[0]));
            }
        }

        return false;
    }

    private static bool IsOrderingMethod(MethodInfo method)
    {
        return method.DeclaringType == typeof(Queryable) && method.Name is nameof(Queryable.OrderBy)
            or nameof(Queryable.OrderByDescending)
            or nameof(Queryable.ThenBy)
            or nameof(Queryable.ThenByDescending);
    }

    private static Expression Unwrap(Expression expression)
    {
        return expression is UnaryExpression { NodeType: ExpressionType.Quote, Operand: var operand } ? operand : expression;
    }
}
