namespace Vitarish.Financial.Domain.Models;

public readonly record struct Money(long MinorUnits, string Currency, int Exponent)
{
    public static Money Create(long minorUnits, string currency, int exponent) => string.IsNullOrWhiteSpace(currency)
            ? throw new ArgumentException("Currency is required.", nameof(currency))
            : exponent is < 0 or > 9
            ? throw new ArgumentOutOfRangeException(nameof(exponent), "Exponent must be between 0 and 9.")
            : new Money(minorUnits, currency.Trim().ToUpperInvariant(), exponent);

    public void EnsurePositive(string? paramName = null)
    {
        if (MinorUnits <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName ?? nameof(MinorUnits), "Amount must be greater than zero.");
        }
    }
}
