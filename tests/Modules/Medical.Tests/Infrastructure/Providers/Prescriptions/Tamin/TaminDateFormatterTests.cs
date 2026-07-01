using FluentAssertions;
using Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

namespace Medical.Tests.Infrastructure.Providers.Prescriptions.Tamin;

public sealed class TaminDateFormatterTests
{
    private readonly TaminDateFormatter _formatter = new();

    [Theory]
    [InlineData(2026, 6, 1, "14050311")]
    [InlineData(2024, 8, 22, "14030601")]
    [InlineData(2026, 3, 21, "14050101")]
    public void FormatDate_returns_eight_digit_jalali_provider_date(int year, int month, int day, string expected)
    {
        string result = _formatter.FormatDate(new DateOnly(year, month, day));

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("1403-06-01", false)]
    [InlineData("2024-08-22", false)]
    [InlineData("14030601", true)]
    public void IsValidProviderDate_requires_eight_digits(string? value, bool expected)
    {
        bool result = _formatter.IsValidProviderDate(value);

        result.Should().Be(expected);
    }
}
