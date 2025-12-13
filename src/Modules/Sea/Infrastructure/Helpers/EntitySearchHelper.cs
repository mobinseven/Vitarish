using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Vitarish.Sea.Infrastructure.Helpers;

internal static class EntitySearchHelper
{
    private static readonly char[] SearchSeparators = [' ', '\t', '\r', '\n'];

    private static readonly (string Variant, string Replacement)[] NormalizationPairs =
    [
        ("\u200c", string.Empty), // Zero-width non-joiner
        ("ك", "ک"),
        ("ي", "ی"),
        ("ى", "ی"),
        ("ئ", "ی"),
        ("ة", "ه"),
        ("ۀ", "ه"),
        ("أ", "ا"),
        ("إ", "ا"),
        ("آ", "ا"),
        ("ؤ", "و")
    ];

    private static readonly IReadOnlyDictionary<char, string> NormalizationLookup =
        NormalizationPairs
            .Where(pair => pair.Variant.Length == 1)
            .ToDictionary(pair => pair.Variant[0], pair => pair.Replacement);

    private static readonly MethodInfo StringReplaceMethod = typeof(string)
        .GetMethod(nameof(string.Replace), [typeof(string), typeof(string)])!;

    public static IReadOnlyCollection<string> CreateSearchPatterns(string search)
    {
        ArgumentNullException.ThrowIfNull(search);

        HashSet<string> fragments = new(StringComparer.OrdinalIgnoreCase);

        string trimmed = NormalizeText(search.Trim());
        if(trimmed.Length > 0)
        {
            _ = fragments.Add(trimmed);
        }

        foreach(string fragment in search.Split(SearchSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string normalized = NormalizeText(fragment);
            if(normalized.Length > 0)
            {
                _ = fragments.Add(normalized);
            }
        }

        return fragments.Select(static fragment => $"%{EscapeLikeFragment(fragment)}%").ToArray();
    }

    public static Expression<Func<TEntity, bool>> BuildSearchPredicate<TEntity>(
        IEnumerable<string> patterns,
        params Expression<Func<TEntity, string?>>[] propertySelectors)
    {
        ArgumentNullException.ThrowIfNull(patterns);
        ArgumentNullException.ThrowIfNull(propertySelectors);

        using IEnumerator<string> enumerator = patterns.GetEnumerator();
        if(!enumerator.MoveNext() || propertySelectors.Length == 0)
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

            foreach(Expression<Func<TEntity, string?>> selector in propertySelectors)
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

            if(patternPredicate is null)
            {
                continue;
            }

            predicate = predicate is null ? patternPredicate : Expression.OrElse(predicate, patternPredicate);
        }
        while(enumerator.MoveNext());

        return predicate is null
            ? static _ => true
            : Expression.Lambda<Func<TEntity, bool>>(predicate, entity);
    }

    public static string NormalizeText(string value)
    {
        if(string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string normalized = value.Normalize(NormalizationForm.FormKC);
        StringBuilder builder = new(normalized.Length);

        foreach(char character in normalized)
        {
            _ = NormalizationLookup.TryGetValue(character, out string? replacement) ? builder.Append(replacement) : builder.Append(character);
        }

        return builder.ToString();
    }

    public static Expression NormalizeProperty(Expression property)
    {
        Expression normalized = property;

        foreach((string? variant, string? replacement) in NormalizationPairs)
        {
            normalized = Expression.Call(
                normalized,
                StringReplaceMethod,
                Expression.Constant(variant),
                Expression.Constant(replacement));
        }

        return normalized;
    }

    private static string EscapeLikeFragment(string fragment)
    {
        return fragment
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal);
    }

    private sealed class ReplaceParameterVisitor(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == source ? target : base.VisitParameter(node);
        }
    }
}
