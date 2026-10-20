using FluentAssertions;
using Vitarish.Parties.Domain.Profiles;

namespace Vitarish.Parties.Tests.Domain;

public sealed class TenantBusinessDetailsTests
{
    private static readonly TimeOnly Nine = new(9, 0);
    private static readonly TimeOnly One = new(13, 0);
    private static readonly TimeOnly Four = new(16, 0);
    private static readonly TimeOnly Eight = new(20, 0);

    [Fact]
    public void Normalizes_text_drops_blank_entries_and_sorts_days_and_periods()
    {
        TenantBusinessDetails? details = TenantBusinessDetails.TryCreate(
            new TenantBusinessAddress("  Tehran ", " ", null, "", 35.7, 51.4),
            [" 021 1234 5678 ", "  ", null],
            " https://rezaei.example.test ",
            ["https://instagram.com/rezaei", ""],
            [
                new TenantWorkingDay(DayOfWeek.Sunday, [new(Four, Eight), new(Nine, One)]),
                new TenantWorkingDay(DayOfWeek.Saturday, [])
            ],
            null,
            out string? error);

        error.Should().BeNull();
        details!.Address.Should().Be(new TenantBusinessAddress("Tehran", null, null, null, 35.7, 51.4));
        details.Phones.Should().Equal("021 1234 5678");
        details.Website.Should().Be("https://rezaei.example.test");
        details.SocialLinks.Should().Equal("https://instagram.com/rezaei");
        details.WorkingHours.Select(day => day.Day).Should().Equal(DayOfWeek.Sunday, DayOfWeek.Saturday);
        details.WorkingHours[0].Intervals.Should().Equal(new TenantOpeningInterval(Nine, One), new TenantOpeningInterval(Four, Eight));
        details.WorkingHours[1].Intervals.Should().BeEmpty("an empty day means closed");
        details.HasPhotos.Should().BeFalse();
    }

    [Fact]
    public void An_address_with_nothing_in_it_is_no_address()
    {
        TenantBusinessDetails? details = TenantBusinessDetails.TryCreate(new TenantBusinessAddress(" ", null, "", null, null, null), null, null, null, null, null, out string? error);

        error.Should().BeNull();
        details!.Address.Should().BeNull();
    }

    public static TheoryData<string, TenantBusinessDetailsInput> InvalidInputs => new()
    {
        { "pin needs both coordinates", new() { Address = new(null, null, null, null, 35.7, null) } },
        { "latitude out of range", new() { Address = new(null, null, null, null, 91, 51.4) } },
        { "longitude out of range", new() { Address = new(null, null, null, null, 35.7, -181) } },
        { "postal code letters", new() { Address = new(null, null, null, "AB-12", null, null) } },
        { "street too long", new() { Address = new(null, null, new string('x', TenantBusinessDetails.StreetMaximumLength + 1), null, null, null) } },
        { "phone with letters", new() { Phones = ["call me"] } },
        { "phone too short", new() { Phones = ["123"] } },
        { "plus inside a phone", new() { Phones = ["021+1234"] } },
        { "too many phones", new() { Phones = [.. Enumerable.Repeat("021 1234 5678", TenantBusinessDetails.MaximumPhones + 1)] } },
        { "script website", new() { Website = "javascript:alert(1)" } },
        { "relative website", new() { Website = "rezaei.example.test" } },
        { "ftp social link", new() { SocialLinks = ["ftp://files.example.test"] } },
        { "too many social links", new() { SocialLinks = [.. Enumerable.Repeat("https://example.test", TenantBusinessDetails.MaximumSocialLinks + 1)] } },
        { "day listed twice", new() { WorkingHours = [new(DayOfWeek.Monday, []), new(DayOfWeek.Monday, [])] } },
        { "unknown weekday", new() { WorkingHours = [new((DayOfWeek)7, [])] } },
        { "closes before it opens", new() { WorkingHours = [new(DayOfWeek.Monday, [new(One, Nine)])] } },
        { "closes when it opens", new() { WorkingHours = [new(DayOfWeek.Monday, [new(Nine, Nine)])] } },
        { "overlapping periods", new() { WorkingHours = [new(DayOfWeek.Monday, [new(Nine, Four), new(One, Eight)])] } },
        { "too many periods", new() { WorkingHours = [new(DayOfWeek.Monday, [new(new(6, 0), new(7, 0)), new(new(8, 0), new(9, 0)), new(new(10, 0), new(11, 0)), new(new(12, 0), new(13, 0))])] } },
        { "empty photo id", new() { PhotoFileIds = [Guid.Empty] } },
        { "duplicate photo", new() { PhotoFileIds = [SamePhoto, SamePhoto] } },
        { "too many photos", new() { PhotoFileIds = [.. Enumerable.Range(0, TenantBusinessDetails.MaximumPhotos + 1).Select(_ => Guid.NewGuid())] } }
    };

    private static readonly Guid SamePhoto = Guid.NewGuid();

    [Theory]
    [MemberData(nameof(InvalidInputs))]
    public void Rejects_invalid_details_with_a_reason(string because, TenantBusinessDetailsInput input)
    {
        TenantBusinessDetails? details = TenantBusinessDetails.TryCreate(
            input.Address, input.Phones, input.Website, input.SocialLinks, input.WorkingHours, input.PhotoFileIds, out string? error);

        details.Should().BeNull(because);
        error.Should().NotBeNullOrWhiteSpace(because);
    }

    [Fact]
    public void Allows_adjacent_periods_and_the_maximum_counts()
    {
        TenantBusinessDetails? details = TenantBusinessDetails.TryCreate(
            null,
            [.. Enumerable.Repeat("+98 21 1234 5678", TenantBusinessDetails.MaximumPhones)],
            "http://rezaei.example.test",
            null,
            [new TenantWorkingDay(DayOfWeek.Monday, [new(Nine, One), new(One, Four), new(Four, Eight)])],
            [.. Enumerable.Range(0, TenantBusinessDetails.MaximumPhotos).Select(_ => Guid.NewGuid())],
            out string? error);

        error.Should().BeNull();
        details.Should().NotBeNull();
    }

    [Fact]
    public void Equivalence_compares_contents_not_list_instances()
    {
        Guid photo = Guid.NewGuid();
        TenantBusinessDetails first = Create("021 1234 5678", photo);
        TenantBusinessDetails same = Create("021 1234 5678", photo);
        TenantBusinessDetails otherPhone = Create("021 9999 0000", photo);
        TenantBusinessDetails otherPhoto = Create("021 1234 5678", Guid.NewGuid());

        first.IsEquivalentTo(same).Should().BeTrue();
        first.IsEquivalentTo(otherPhone).Should().BeFalse();
        first.IsEquivalentTo(otherPhoto).Should().BeFalse();
        first.IsEquivalentTo(null).Should().BeFalse();
    }

    private static TenantBusinessDetails Create(string phone, Guid photo) =>
        TenantBusinessDetails.TryCreate(
            new TenantBusinessAddress("Tehran", null, null, null, null, null),
            [phone],
            null,
            null,
            [new TenantWorkingDay(DayOfWeek.Monday, [new(Nine, One)])],
            [photo],
            out _)!;

    public sealed record TenantBusinessDetailsInput
    {
        public TenantBusinessAddress? Address { get; init; }
        public string?[]? Phones { get; init; }
        public string? Website { get; init; }
        public string?[]? SocialLinks { get; init; }
        public TenantWorkingDay[]? WorkingHours { get; init; }
        public Guid[]? PhotoFileIds { get; init; }
    }
}
