using System.Globalization;
using Vitarish.Parties.Application.Profiles;
using Vitarish.Parties.Domain.Profiles;
using C = Vitarish.Parties.Contracts.Tenants;

namespace Vitarish.Parties.Api;

/// <summary>Maps business profile wire shapes to and from the domain.</summary>
internal static class TenantBusinessProfileMapping
{
    private const string TimeFormat = "HH:mm";

    /// <summary>Null details map to null; <paramref name="error"/> says why the details are invalid.</summary>
    public static TenantBusinessDetails? ToDomain(C.TenantBusinessDetailsDto? dto, out string? error)
    {
        error = null;
        if (dto is null)
            return null;

        List<TenantWorkingDay> days = [];
        foreach (C.TenantWorkingDayDto day in dto.WorkingHours ?? [])
        {
            List<TenantOpeningInterval> intervals = [];
            foreach (C.TenantOpeningIntervalDto interval in day.Intervals ?? [])
            {
                if (!TryParseTime(interval.Opens, out TimeOnly opens) || !TryParseTime(interval.Closes, out TimeOnly closes))
                {
                    error = $"Opening times use the {TimeFormat} format.";
                    return null;
                }
                intervals.Add(new TenantOpeningInterval(opens, closes));
            }
            days.Add(new TenantWorkingDay(day.Day, intervals));
        }

        TenantBusinessAddress? address = dto.Address is { } wire
            ? new TenantBusinessAddress(wire.Province, wire.City, wire.Street, wire.PostalCode, wire.Latitude, wire.Longitude)
            : null;
        return TenantBusinessDetails.TryCreate(address, dto.Phones, dto.Website, dto.SocialLinks, days, dto.PhotoFileIds, out error);
    }

    public static C.TenantBusinessProfileResponse ToContract(TenantBusinessProfileView view) =>
        new(view.Name, view.Description, view.LogoUrl, view.Category, view.Details is { } details ? ToContract(details) : null);

    private static C.TenantBusinessDetailsDto ToContract(TenantBusinessDetails details) => new(
        details.Address is { } address
            ? new C.TenantBusinessAddressDto(address.Province, address.City, address.Street, address.PostalCode, address.Latitude, address.Longitude)
            : null,
        details.Phones,
        details.Website,
        details.SocialLinks,
        [.. details.WorkingHours.Select(day => new C.TenantWorkingDayDto(
            day.Day,
            [.. day.Intervals.Select(interval => new C.TenantOpeningIntervalDto(
                interval.Opens.ToString(TimeFormat, CultureInfo.InvariantCulture),
                interval.Closes.ToString(TimeFormat, CultureInfo.InvariantCulture)))]))],
        details.PhotoFileIds);

    private static bool TryParseTime(string? value, out TimeOnly time) =>
        TimeOnly.TryParseExact(value, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
}
