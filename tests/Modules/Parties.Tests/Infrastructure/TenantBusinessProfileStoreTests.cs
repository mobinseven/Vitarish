using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Vitarish.Parties.Application.Profiles;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Profiles;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Services;

namespace Vitarish.Parties.Tests.Infrastructure;

public sealed class TenantBusinessProfileStoreTests
{
    private static readonly DateTime FixedNow = new(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Saves_then_replaces_the_profile_and_reads_back_every_detail()
    {
        string database = Guid.NewGuid().ToString("N");
        Guid tenantId = await SeedTenantAsync(database);
        Guid photo = Guid.NewGuid();
        TenantBusinessDetails details = Details(photo);

        await using (PartiesDbContext db = CreateContext(database))
            (await new TenantBusinessProfileStore(db).SaveAsync(tenantId, "Skin care", "dermatology", details, FixedNow)).Should().BeTrue();
        await using (PartiesDbContext db = CreateContext(database))
            (await new TenantBusinessProfileStore(db).SaveAsync(tenantId, null, "other", null, FixedNow.AddMinutes(1))).Should().BeTrue();
        await using (PartiesDbContext db = CreateContext(database))
            (await new TenantBusinessProfileStore(db).SaveAsync(tenantId, "Skin care", "dermatology", details, FixedNow.AddMinutes(2))).Should().BeTrue();

        await using PartiesDbContext verify = CreateContext(database);
        TenantBusinessProfileView? view = await new TenantBusinessProfileStore(verify).GetAsync(tenantId);
        view.Should().NotBeNull();
        view!.Name.Should().Be("Rezaei Clinic");
        view.Description.Should().Be("Skin care");
        view.Category.Should().Be("dermatology");
        view.Details!.IsEquivalentTo(details).Should().BeTrue();
        view.Details.WorkingHours.Single().Intervals.Should().Equal(new TenantOpeningInterval(new TimeOnly(9, 0), new TimeOnly(13, 0)));
        (await verify.TenantBusinessProfiles.SingleAsync()).UpdatedAt.Should().Be(FixedNow.AddMinutes(2));
    }

    [Fact]
    public async Task Replacing_the_whole_tenant_row_as_administration_does_keeps_the_owners_profile()
    {
        string database = Guid.NewGuid().ToString("N");
        Guid tenantId = await SeedTenantAsync(database);
        await using (PartiesDbContext db = CreateContext(database))
            _ = await new TenantBusinessProfileStore(db).SaveAsync(tenantId, null, "dermatology", Details(Guid.NewGuid()), FixedNow);

        await using (PartiesDbContext db = CreateContext(database))
        {
            // Administration maps its write DTO to a new entity and replaces the row.
            _ = db.Tenants.Update(new Tenant { Id = tenantId, OwnerId = Guid.NewGuid(), Name = "Renamed", AppId = "clinic-app" });
            _ = await db.SaveChangesAsync();
        }

        await using PartiesDbContext verify = CreateContext(database);
        TenantBusinessProfileView? view = await new TenantBusinessProfileStore(verify).GetAsync(tenantId);
        view!.Name.Should().Be("Renamed");
        view.Category.Should().Be("dermatology");
        view.Details.Should().NotBeNull();
    }

    [Fact]
    public async Task Sets_the_logo_url_and_reports_missing_or_deleted_tenants()
    {
        string database = Guid.NewGuid().ToString("N");
        Guid tenantId = await SeedTenantAsync(database);
        Guid deletedId = await SeedTenantAsync(database, deleted: true);

        await using PartiesDbContext db = CreateContext(database);
        TenantBusinessProfileStore store = new(db);

        (await store.SetLogoUrlAsync(tenantId, "/api/modules/files/logo/content")).Should().BeTrue();
        (await store.SetLogoUrlAsync(Guid.NewGuid(), "/x")).Should().BeFalse();
        (await store.SaveAsync(deletedId, null, null, null, FixedNow)).Should().BeFalse();
        (await store.GetAsync(deletedId)).Should().BeNull();
        (await store.GetAsync(tenantId))!.LogoUrl.Should().Be("/api/modules/files/logo/content");
    }

    [Fact]
    public void Stores_details_as_camel_case_json_with_named_weekdays_and_times()
    {
        Guid photo = Guid.Parse("11111111-2222-3333-4444-555555555555");
        using PartiesDbContext db = CreateContext(Guid.NewGuid().ToString("N"));
        Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter converter = db.Model
            .FindEntityType(typeof(TenantBusinessProfile))!
            .FindProperty(nameof(TenantBusinessProfile.Details))!
            .GetValueConverter()!;

        string json = (string)converter.ConvertToProvider(Details(photo))!;

        json.Should().Be(
            "{\"address\":{\"province\":\"Tehran\",\"city\":\"Tehran\",\"street\":\"Valiasr St.\",\"postalCode\":\"1234567890\",\"latitude\":35.7,\"longitude\":51.4},"
            + "\"phones\":[\"021 1234 5678\"],\"website\":\"https://rezaei.example.test\",\"socialLinks\":[\"https://instagram.com/rezaei\"],"
            + "\"workingHours\":[{\"day\":\"Saturday\",\"intervals\":[{\"opens\":\"09:00:00\",\"closes\":\"13:00:00\"}]}],"
            + "\"photoFileIds\":[\"11111111-2222-3333-4444-555555555555\"]}");
        ((TenantBusinessDetails)converter.ConvertFromProvider(json)!).IsEquivalentTo(Details(photo)).Should().BeTrue();
    }

    private static TenantBusinessDetails Details(Guid photo) =>
        TenantBusinessDetails.TryCreate(
            new TenantBusinessAddress("Tehran", "Tehran", "Valiasr St.", "1234567890", 35.7, 51.4),
            ["021 1234 5678"],
            "https://rezaei.example.test",
            ["https://instagram.com/rezaei"],
            [new TenantWorkingDay(DayOfWeek.Saturday, [new TenantOpeningInterval(new TimeOnly(9, 0), new TimeOnly(13, 0))])],
            [photo],
            out _)!;

    private static async Task<Guid> SeedTenantAsync(string database, bool deleted = false)
    {
        Guid tenantId = Guid.NewGuid();
        await using PartiesDbContext db = CreateContext(database);
        _ = db.Tenants.Add(new Tenant { Id = tenantId, OwnerId = Guid.NewGuid(), Name = "Rezaei Clinic", AppId = "clinic-app", DeletedAt = deleted ? FixedNow : null });
        _ = await db.SaveChangesAsync();
        return tenantId;
    }

    private static PartiesDbContext CreateContext(string database) => new(
        new DbContextOptionsBuilder<PartiesDbContext>().UseInMemoryDatabase(database).Options,
        Mock.Of<IPublisher>(),
        Array.Empty<IInterceptor>());
}
