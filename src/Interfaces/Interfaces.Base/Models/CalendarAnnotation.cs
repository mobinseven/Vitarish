namespace Vitarish.Interfaces.Base.Models;

public sealed record CalendarAnnotation(
    DateOnly StartDate,
    DateOnly EndDate,
    string? Label = null,
    string? HighlightClass = null,
    string? HighlightStyle = null,
    string? Source = null)
{
    public bool Includes(DateOnly date) => date >= StartDate && date <= EndDate;
}
