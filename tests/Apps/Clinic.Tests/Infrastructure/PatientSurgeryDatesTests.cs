using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Vitarish.Clinic.Domain.Enums;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Clinic.Domain.Surgery;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Clinic.Infrastructure.Handlers;
using Vitarish.Entities.Server.Infrastructure.Search;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Clinic.Tests.Infrastructure;

public sealed class PatientSurgeryDatesTests
{
    private const string TenantId = "tenant-1";
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid WithBoth = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid OnlyCancelled = Guid.Parse("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid OnlyUpcoming = Guid.Parse("00000000-0000-0000-0000-00000000000c");

    [Theory]
    [InlineData(PatientSortBy.LastSurgery, false)]
    [InlineData(PatientSortBy.LastSurgery, true)]
    [InlineData(PatientSortBy.NextSurgery, false)]
    [InlineData(PatientSortBy.NextSurgery, true)]
    public void Surgery_date_sorts_translate_to_postgresql(PatientSortBy sortBy, bool descending)
    {
        DbContextOptions<ClinicDbContext> options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseNpgsql("Host=localhost;Database=translation-only")
            .Options;
        using ClinicDbContext context = new(options, new TestRequestContext(), new HttpContextAccessor());

        string sql = CreateShape().ApplySort(context.PatientViews.AsNoTracking(), sortBy, descending, context, Now).ToQueryString();

        sql.Should().Contain("mv_PatientSurgeries").And.Contain("ORDER BY");
    }

    [Fact]
    public async Task Loader_sets_the_last_non_cancelled_and_next_planned_surgery_per_patient()
    {
        await using TestClinicDbContext context = await SeedAsync();
        List<PatientView> patients = await context.PatientViews.OrderBy(patient => patient.Id).ToListAsync();

        await new PatientSurgeryDatesLoader(context, new FakeTimeProvider(Now)).LoadAsync(patients, CancellationToken.None);

        PatientView both = patients.Single(patient => patient.Id == WithBoth);
        both.LastSurgeryAt.Should().Be(Now.AddDays(-3));
        both.NextSurgeryAt.Should().Be(Now.AddDays(2));
        PatientView cancelledOnly = patients.Single(patient => patient.Id == OnlyCancelled);
        cancelledOnly.LastSurgeryAt.Should().BeNull();
        cancelledOnly.NextSurgeryAt.Should().BeNull();
        PatientView upcomingOnly = patients.Single(patient => patient.Id == OnlyUpcoming);
        upcomingOnly.LastSurgeryAt.Should().BeNull();
        upcomingOnly.NextSurgeryAt.Should().Be(Now.AddDays(1));
    }

    [Fact]
    public async Task Next_surgery_sort_puts_the_soonest_first_and_patients_without_one_last()
    {
        await using TestClinicDbContext context = await SeedAsync();

        List<Guid> ordered = await CreateShape()
            .ApplySort(context.PatientViews, PatientSortBy.NextSurgery, false, context, Now)
            .Select(patient => patient.Id)
            .ToListAsync();

        ordered.Should().Equal(OnlyUpcoming, WithBoth, OnlyCancelled);
    }

    private static PatientViewQueryShape CreateShape() => new(new EntitySearchPatternFactory(new EntitySearchTextNormalizer()));

    private static async Task<TestClinicDbContext> SeedAsync()
    {
        DbContextOptions<ClinicDbContext> options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        TestClinicDbContext context = new(options, new TestRequestContext(), new HttpContextAccessor());
        context.PatientViews.AddRange(
            new PatientView { Id = WithBoth, TenantId = TenantId, Name = "A" },
            new PatientView { Id = OnlyCancelled, TenantId = TenantId, Name = "B" },
            new PatientView { Id = OnlyUpcoming, TenantId = TenantId, Name = "C" });
        context.SurgeryViews.AddRange(
            Surgery(WithBoth, SurgeryStatus.Completed, Now.AddDays(-10)),
            Surgery(WithBoth, SurgeryStatus.Planned, Now.AddDays(-3)),
            Surgery(WithBoth, SurgeryStatus.Cancelled, Now.AddDays(-1)),
            Surgery(WithBoth, SurgeryStatus.Planned, Now.AddDays(2)),
            Surgery(WithBoth, SurgeryStatus.Planned, Now.AddDays(5)),
            Surgery(OnlyCancelled, SurgeryStatus.Cancelled, Now.AddDays(-2)),
            Surgery(OnlyCancelled, SurgeryStatus.Cancelled, Now.AddDays(4)),
            Surgery(OnlyUpcoming, SurgeryStatus.Planned, Now.AddDays(1)));
        await context.SaveChangesAsync();
        return context;
    }

    private static SurgeryView Surgery(Guid patientId, SurgeryStatus status, DateTimeOffset scheduledAt) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = TenantId,
        PatientId = patientId,
        ObjectType = SurgeryObjectTypes.ObjectType,
        Status = status.ToString(),
        ScheduledAt = scheduledAt,
        Data = JsonDocument.Parse("{}"),
        Team = JsonDocument.Parse("[]"),
        CreatedAt = DateTime.UtcNow
    };

    private sealed class TestClinicDbContext(
        DbContextOptions<ClinicDbContext> options,
        IRequestContext requestContext,
        IHttpContextAccessor httpContextAccessor)
        : ClinicDbContext(options, requestContext, httpContextAccessor)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            _ = modelBuilder.Entity<PatientView>().HasKey(patient => patient.Id);
            _ = modelBuilder.Entity<SurgeryView>().HasKey(surgery => surgery.Id);
            _ = modelBuilder.Entity<SurgerySuggestionView>().HasKey(suggestion => new { suggestion.TenantId, suggestion.Kind, suggestion.Value });
        }
    }

    private sealed class TestRequestContext : IRequestContext
    {
        public string Authority => "clinic.tests";
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => null;
        public string? TenantId => PatientSurgeryDatesTests.TenantId;
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => "patient-surgery-dates-test";
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => null;
        public Request GetRequest() => new() { RequestId = RequestId, Authority = Authority, TenantId = TenantId };
    }
}
