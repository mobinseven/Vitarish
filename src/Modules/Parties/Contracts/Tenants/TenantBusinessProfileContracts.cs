using System.Text.Json.Serialization;

namespace Vitarish.Parties.Contracts.Tenants;

/// <param name="Latitude">With <paramref name="Longitude"/>, the map pin; both or neither.</param>
public sealed record TenantBusinessAddressDto(
    string? Province,
    string? City,
    string? Street,
    string? PostalCode,
    double? Latitude,
    double? Longitude);

/// <param name="Opens">Local time as <c>HH:mm</c>.</param>
/// <param name="Closes">Local time as <c>HH:mm</c>, after <paramref name="Opens"/> on the same day.</param>
public sealed record TenantOpeningIntervalDto(string Opens, string Closes);

/// <param name="Intervals">Empty when closed that day.</param>
public sealed record TenantWorkingDayDto(
    [property: JsonConverter(typeof(JsonStringEnumConverter<DayOfWeek>))] DayOfWeek Day,
    IReadOnlyList<TenantOpeningIntervalDto> Intervals);

/// <summary>Public listing details a tenant owner shares about their business. Every part is optional.</summary>
/// <param name="WorkingHours">At most one entry per weekday; a missing weekday is unknown.</param>
/// <param name="PhotoFileIds">Images uploaded to this tenant through Files.</param>
public sealed record TenantBusinessDetailsDto(
    TenantBusinessAddressDto? Address,
    IReadOnlyList<string>? Phones,
    string? Website,
    IReadOnlyList<string>? SocialLinks,
    IReadOnlyList<TenantWorkingDayDto>? WorkingHours,
    IReadOnlyList<Guid>? PhotoFileIds);

/// <summary>The current tenant's business profile, as its owner sees and edits it.</summary>
public sealed record TenantBusinessProfileResponse(
    string Name,
    string? Description,
    string? LogoUrl,
    string? Category,
    TenantBusinessDetailsDto? BusinessDetails);

/// <summary>Replaces the current tenant's description, category, and business details.</summary>
public sealed record UpdateTenantBusinessProfileRequest(
    string? Description,
    string? Category,
    TenantBusinessDetailsDto? BusinessDetails);

/// <summary>Makes one of the current tenant's uploaded images its logo.</summary>
public sealed record SetTenantLogoRequest(Guid FileId);

/// <summary>The business category codes new tenants can choose from.</summary>
public sealed record TenantCategoriesResponse(IReadOnlyList<string> Categories);
