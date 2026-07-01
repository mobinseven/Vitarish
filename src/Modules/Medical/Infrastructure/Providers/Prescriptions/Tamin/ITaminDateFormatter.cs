namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal interface ITaminDateFormatter
{
    string FormatDate(DateTime date);
    string FormatDate(DateOnly date);
    bool IsValidProviderDate(string? value);
}
