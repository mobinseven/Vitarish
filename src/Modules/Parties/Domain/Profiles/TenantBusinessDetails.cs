namespace Vitarish.Parties.Domain.Profiles;

/// <summary>Where a tenant's business is. Every part is optional; a map pin needs both coordinates.</summary>
public sealed record TenantBusinessAddress(
    string? Province,
    string? City,
    string? Street,
    string? PostalCode,
    double? Latitude,
    double? Longitude);

/// <summary>One opening period within a day, e.g. 09:00–13:00.</summary>
public sealed record TenantOpeningInterval(TimeOnly Opens, TimeOnly Closes);

/// <summary>A weekday's opening periods. No periods means closed that day; a missing day is unknown.</summary>
public sealed record TenantWorkingDay(DayOfWeek Day, IReadOnlyList<TenantOpeningInterval> Intervals);

/// <summary>
/// The public listing details a tenant owner can share about their business, similar to a map
/// listing: address and pin, contact, weekly opening hours, and photos. Every part is optional.
/// Use <see cref="TryCreate"/> so the details are always normalized and valid.
/// </summary>
public sealed record TenantBusinessDetails
{
    public const int RegionMaximumLength = 60;
    public const int StreetMaximumLength = 200;
    public const int PostalCodeMaximumLength = 20;
    public const int MaximumPhones = 5;
    public const int PhoneMinimumLength = 4;
    public const int PhoneMaximumLength = 20;
    public const int UrlMaximumLength = 200;
    public const int MaximumSocialLinks = 5;
    public const int MaximumIntervalsPerDay = 3;
    public const int MaximumPhotos = 10;

    private TenantBusinessDetails(
        TenantBusinessAddress? address,
        IReadOnlyList<string> phones,
        string? website,
        IReadOnlyList<string> socialLinks,
        IReadOnlyList<TenantWorkingDay> workingHours,
        IReadOnlyList<Guid> photoFileIds)
    {
        Address = address;
        Phones = phones;
        Website = website;
        SocialLinks = socialLinks;
        WorkingHours = workingHours;
        PhotoFileIds = photoFileIds;
    }

    public TenantBusinessAddress? Address { get; }
    public IReadOnlyList<string> Phones { get; }
    public string? Website { get; }
    public IReadOnlyList<string> SocialLinks { get; }

    /// <summary>At most one entry per weekday, each with sorted, non-overlapping periods.</summary>
    public IReadOnlyList<TenantWorkingDay> WorkingHours { get; }

    /// <summary>Files module ids of the tenant's own photos.</summary>
    public IReadOnlyList<Guid> PhotoFileIds { get; }

    public bool HasPhotos => PhotoFileIds.Count > 0;

    /// <summary>Normalizes and validates the details.</summary>
    /// <param name="error">Why the details are invalid; null when they are valid.</param>
    public static TenantBusinessDetails? TryCreate(
        TenantBusinessAddress? address,
        IEnumerable<string?>? phones,
        string? website,
        IEnumerable<string?>? socialLinks,
        IEnumerable<TenantWorkingDay>? workingHours,
        IEnumerable<Guid>? photoFileIds,
        out string? error)
    {
        error = null;
        TenantBusinessAddress? normalizedAddress = NormalizeAddress(address, ref error);
        string[] normalizedPhones = NormalizeList(phones);
        if (normalizedPhones.Length > MaximumPhones)
            error ??= $"At most {MaximumPhones} phone numbers are allowed.";
        if (normalizedPhones.Any(phone => !IsPhone(phone)))
            error ??= $"A phone number has {PhoneMinimumLength} to {PhoneMaximumLength} digits, spaces, hyphens, parentheses, or a leading plus.";
        string? normalizedWebsite = Trimmed(website);
        if (normalizedWebsite is not null && !IsWebAddress(normalizedWebsite))
            error ??= $"The website must be an http or https address of at most {UrlMaximumLength} characters.";
        string[] normalizedLinks = NormalizeList(socialLinks);
        if (normalizedLinks.Length > MaximumSocialLinks)
            error ??= $"At most {MaximumSocialLinks} social links are allowed.";
        if (normalizedLinks.Any(link => !IsWebAddress(link)))
            error ??= $"Each social link must be an http or https address of at most {UrlMaximumLength} characters.";
        TenantWorkingDay[] normalizedHours = NormalizeWorkingHours(workingHours, ref error);
        Guid[] photos = photoFileIds?.ToArray() ?? [];
        if (photos.Length > MaximumPhotos)
            error ??= $"At most {MaximumPhotos} photos are allowed.";
        if (photos.Any(photo => photo == Guid.Empty) || photos.Distinct().Count() != photos.Length)
            error ??= "Each photo must be a distinct file.";

        return error is null
            ? new TenantBusinessDetails(normalizedAddress, normalizedPhones, normalizedWebsite, normalizedLinks, normalizedHours, photos)
            : null;
    }

    /// <summary>
    /// Restores details that were valid when stored, without re-applying today's rules, so a
    /// later tightening never makes stored listings unreadable. Only persistence uses this.
    /// </summary>
    public static TenantBusinessDetails Restore(
        TenantBusinessAddress? address,
        IReadOnlyList<string> phones,
        string? website,
        IReadOnlyList<string> socialLinks,
        IReadOnlyList<TenantWorkingDay> workingHours,
        IReadOnlyList<Guid> photoFileIds) =>
        new(address, phones, website, socialLinks, workingHours, photoFileIds);

    /// <summary>True when both describe the same business details.</summary>
    public bool IsEquivalentTo(TenantBusinessDetails? other) =>
        other is not null
        && Address == other.Address
        && Phones.SequenceEqual(other.Phones)
        && Website == other.Website
        && SocialLinks.SequenceEqual(other.SocialLinks)
        && WorkingHours.Count == other.WorkingHours.Count
        && WorkingHours.Zip(other.WorkingHours).All(pair => pair.First.Day == pair.Second.Day && pair.First.Intervals.SequenceEqual(pair.Second.Intervals))
        && PhotoFileIds.SequenceEqual(other.PhotoFileIds);

    private static TenantBusinessAddress? NormalizeAddress(TenantBusinessAddress? address, ref string? error)
    {
        if (address is null)
            return null;
        TenantBusinessAddress normalized = new(
            Trimmed(address.Province),
            Trimmed(address.City),
            Trimmed(address.Street),
            Trimmed(address.PostalCode),
            address.Latitude,
            address.Longitude);
        if (normalized.Province?.Length > RegionMaximumLength || normalized.City?.Length > RegionMaximumLength)
            error ??= $"The province and city can have at most {RegionMaximumLength} characters.";
        if (normalized.Street?.Length > StreetMaximumLength)
            error ??= $"The street address can have at most {StreetMaximumLength} characters.";
        if (normalized.PostalCode is { } postalCode && (postalCode.Length > PostalCodeMaximumLength || !postalCode.All(character => char.IsAsciiDigit(character) || character == '-')))
            error ??= $"The postal code has at most {PostalCodeMaximumLength} digits or hyphens.";
        if (normalized.Latitude.HasValue != normalized.Longitude.HasValue)
            error ??= "A map location needs both latitude and longitude.";
        if (normalized.Latitude is < -90 or > 90 || normalized.Longitude is < -180 or > 180 || normalized.Latitude is double.NaN || normalized.Longitude is double.NaN)
            error ??= "The map location is outside valid coordinates.";
        return normalized is { Province: null, City: null, Street: null, PostalCode: null, Latitude: null } ? null : normalized;
    }

    private static TenantWorkingDay[] NormalizeWorkingHours(IEnumerable<TenantWorkingDay>? workingHours, ref string? error)
    {
        TenantWorkingDay[] days = workingHours?.ToArray() ?? [];
        if (days.Any(day => !Enum.IsDefined(day.Day)) || days.Select(day => day.Day).Distinct().Count() != days.Length)
            error ??= "Each weekday can appear at most once.";
        List<TenantWorkingDay> normalized = [];
        foreach (TenantWorkingDay day in days.OrderBy(day => day.Day))
        {
            TenantOpeningInterval[] intervals = [.. (day.Intervals ?? []).OrderBy(interval => interval.Opens)];
            if (intervals.Length > MaximumIntervalsPerDay)
                error ??= $"A day can have at most {MaximumIntervalsPerDay} opening periods.";
            if (intervals.Any(interval => interval.Opens >= interval.Closes))
                error ??= "Each opening period must close after it opens on the same day.";
            if (intervals.Zip(intervals.Skip(1)).Any(pair => pair.Second.Opens < pair.First.Closes))
                error ??= "A day's opening periods cannot overlap.";
            normalized.Add(new TenantWorkingDay(day.Day, intervals));
        }
        return [.. normalized];
    }

    private static string[] NormalizeList(IEnumerable<string?>? values) =>
        values is null ? [] : [.. values.Select(Trimmed).OfType<string>()];

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsPhone(string phone) =>
        phone.Length is >= PhoneMinimumLength and <= PhoneMaximumLength
        && phone.Select((character, index) => char.IsAsciiDigit(character) || character is ' ' or '-' or '(' or ')' || (character == '+' && index == 0)).All(valid => valid)
        && phone.Count(char.IsAsciiDigit) >= PhoneMinimumLength;

    private static bool IsWebAddress(string value) =>
        value.Length <= UrlMaximumLength
        && Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && !string.IsNullOrEmpty(uri.Host);
}
