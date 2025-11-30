namespace Vitarish.Base.Domain.Primitives;

/// <summary>
/// Base class for strongly-typed identifiers.
/// </summary>
/// <typeparam name="TValue">The underlying value type of the identifier.</typeparam>
public abstract class StronglyTypedId<TValue> : ValueObject
    where TValue : notnull
{
    /// <summary>
    /// Gets the underlying value of the identifier.
    /// </summary>
    public TValue Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StronglyTypedId{TValue}"/> class.
    /// </summary>
    /// <param name="value">The underlying value.</param>
    protected StronglyTypedId(TValue value)
    {
        Value = value;
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString() ?? string.Empty;
}

/// <summary>
/// Base class for strongly-typed GUID identifiers.
/// </summary>
public abstract class GuidId : StronglyTypedId<Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuidId"/> class.
    /// </summary>
    /// <param name="value">The GUID value.</param>
    protected GuidId(Guid value) : base(value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Id cannot be empty.", nameof(value));
        }
    }
}

/// <summary>
/// Base class for strongly-typed string identifiers.
/// </summary>
public abstract class StringId : StronglyTypedId<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringId"/> class.
    /// </summary>
    /// <param name="value">The string value.</param>
    protected StringId(string value) : base(value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Id cannot be null or whitespace.", nameof(value));
        }
    }
}
