using System.Reflection;

namespace Vitarish.Base.Domain.Primitives;

/// <summary>
/// Base class for enumeration (smart enum) pattern.
/// Provides type-safe enumeration with additional functionality.
/// </summary>
/// <typeparam name="TEnum">The enumeration type.</typeparam>
public abstract class Enumeration<TEnum> : IEquatable<Enumeration<TEnum>>
    where TEnum : Enumeration<TEnum>
{
    private static readonly Lazy<Dictionary<int, TEnum>> EnumerationsByValue = new(GetAllEnumerations);
    private static readonly Lazy<Dictionary<string, TEnum>> EnumerationsByName = new(
        () => GetAllEnumerations().ToDictionary(e => e.Value.Name, e => e.Value, StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Gets the value of the enumeration.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Gets the name of the enumeration.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Enumeration{TEnum}"/> class.
    /// </summary>
    /// <param name="value">The enumeration value.</param>
    /// <param name="name">The enumeration name.</param>
    protected Enumeration(int value, string name)
    {
        Value = value;
        Name = name;
    }

    /// <summary>
    /// Gets all enumeration values.
    /// </summary>
    public static IReadOnlyCollection<TEnum> GetAll() => EnumerationsByValue.Value.Values.ToList();

    /// <summary>
    /// Gets an enumeration by its value.
    /// </summary>
    public static TEnum? FromValue(int value) =>
        EnumerationsByValue.Value.TryGetValue(value, out var enumeration) ? enumeration : null;

    /// <summary>
    /// Gets an enumeration by its name (case-insensitive).
    /// </summary>
    public static TEnum? FromName(string name) =>
        EnumerationsByName.Value.TryGetValue(name, out var enumeration) ? enumeration : null;

    /// <summary>
    /// Tries to get an enumeration by its value.
    /// </summary>
    public static bool TryFromValue(int value, out TEnum? enumeration) =>
        EnumerationsByValue.Value.TryGetValue(value, out enumeration);

    /// <summary>
    /// Tries to get an enumeration by its name.
    /// </summary>
    public static bool TryFromName(string name, out TEnum? enumeration) =>
        EnumerationsByName.Value.TryGetValue(name, out enumeration);

    private static Dictionary<int, TEnum> GetAllEnumerations()
    {
        var enumerationType = typeof(TEnum);
        return enumerationType
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(field => field.FieldType == enumerationType)
            .Select(field => (TEnum)field.GetValue(null)!)
            .ToDictionary(enumeration => enumeration.Value);
    }

    public bool Equals(Enumeration<TEnum>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return GetType() == other.GetType() && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not Enumeration<TEnum> other) return false;
        return Equals(other);
    }

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Name;

    public static bool operator ==(Enumeration<TEnum>? left, Enumeration<TEnum>? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Enumeration<TEnum>? left, Enumeration<TEnum>? right)
    {
        return !(left == right);
    }
}
