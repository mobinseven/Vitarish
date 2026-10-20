using System.Text.Json;
using System.Text.Json.Serialization;
using Vitarish.Parties.Domain.Profiles;

namespace Vitarish.Parties.Infrastructure.Data;

/// <summary>
/// The persisted JSON shape of <see cref="TenantBusinessDetails"/>. Its property names and
/// weekday/time formats are a stored format that later read models query directly, so change
/// it only additively.
/// </summary>
internal sealed record TenantBusinessDetailsDocument(
    TenantBusinessAddressDocument? Address,
    string[] Phones,
    string? Website,
    string[] SocialLinks,
    TenantWorkingDayDocument[] WorkingHours,
    Guid[] PhotoFileIds)
{
    public static string Serialize(TenantBusinessDetails details) => JsonSerializer.Serialize(
        new TenantBusinessDetailsDocument(
            details.Address is { } address
                ? new(address.Province, address.City, address.Street, address.PostalCode, address.Latitude, address.Longitude)
                : null,
            [.. details.Phones],
            details.Website,
            [.. details.SocialLinks],
            [.. details.WorkingHours.Select(day => new TenantWorkingDayDocument(
                day.Day,
                [.. day.Intervals.Select(interval => new TenantOpeningIntervalDocument(interval.Opens, interval.Closes))]))],
            [.. details.PhotoFileIds]),
        TenantBusinessDetailsJsonContext.Default.TenantBusinessDetailsDocument);

    public static TenantBusinessDetails Deserialize(string json)
    {
        TenantBusinessDetailsDocument document = JsonSerializer.Deserialize(json, TenantBusinessDetailsJsonContext.Default.TenantBusinessDetailsDocument)
            ?? throw new JsonException("The stored business details are empty.");
        return TenantBusinessDetails.Restore(
            document.Address is { } address
                ? new(address.Province, address.City, address.Street, address.PostalCode, address.Latitude, address.Longitude)
                : null,
            document.Phones ?? [],
            document.Website,
            document.SocialLinks ?? [],
            [.. (document.WorkingHours ?? []).Select(day => new TenantWorkingDay(
                day.Day,
                [.. (day.Intervals ?? []).Select(interval => new TenantOpeningInterval(interval.Opens, interval.Closes))]))],
            document.PhotoFileIds ?? []);
    }
}

internal sealed record TenantBusinessAddressDocument(string? Province, string? City, string? Street, string? PostalCode, double? Latitude, double? Longitude);

internal sealed record TenantWorkingDayDocument(DayOfWeek Day, TenantOpeningIntervalDocument[] Intervals);

internal sealed record TenantOpeningIntervalDocument(TimeOnly Opens, TimeOnly Closes);

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TenantBusinessDetailsDocument))]
internal sealed partial class TenantBusinessDetailsJsonContext : JsonSerializerContext;
