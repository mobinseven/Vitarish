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

    [Fact]
    public void FormatDate_rejects_default_date_with_clear_argument_error()
    {
        Action act = () => _formatter.FormatDate(default(DateOnly));

        act.Should().Throw<ArgumentException>()
            .WithParameterName("date")
            .WithMessage("Date cannot be the default value.*");
    }

    [Fact]
    public void FormatDate_rejects_dates_outside_persian_calendar_range()
    {
        Action act = () => _formatter.FormatDate(new DateOnly(622, 3, 20));

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("date")
            .WithMessage("Date must be between * for Persian calendar conversion.*");
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
