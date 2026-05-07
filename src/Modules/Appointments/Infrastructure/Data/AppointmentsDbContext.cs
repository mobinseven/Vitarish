using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Appointments.Infrastructure.Data;

public class AppointmentsDbContext : DbContext, IMultiTenantDbContext
{
    private readonly IEnumerable<IInterceptor> _interceptors;
    private readonly IRequestContext _requestContext;

    public ITenantInfo? TenantInfo
    {
        get
        {
            string tenantId = _requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID can not be recognized.");
            return new TenantInfo
            {
                Id = tenantId,
                Identifier = tenantId,
                Name = tenantId
            };
        }
    }

    public TenantMismatchMode TenantMismatchMode { get; set; } = TenantMismatchMode.Throw;
    public TenantNotSetMode TenantNotSetMode { get; set; } = TenantNotSetMode.Throw;

    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionOccurrence> SessionOccurrences => Set<SessionOccurrence>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<CapacityPolicy> CapacityPolicies => Set<CapacityPolicy>();
    public DbSet<BookingPolicy> BookingPolicies => Set<BookingPolicy>();
    public DbSet<SessionReminderPolicy> SessionReminderPolicies => Set<SessionReminderPolicy>();
    public DbSet<AppointmentAuditEntry> AuditEntries => Set<AppointmentAuditEntry>();

    public AppointmentsDbContext(
        DbContextOptions<AppointmentsDbContext> options,
        IEnumerable<IInterceptor> interceptors,
        IRequestContext requestContext) : base(options)
    {
        _interceptors = interceptors;
        _requestContext = requestContext;

        this.EnforceMultiTenantOnTracking();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => _ = optionsBuilder
            .AddInterceptors(_interceptors);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.EnforceMultiTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        this.EnforceMultiTenant();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = modelBuilder.ConfigureMultiTenant();

        ConfigureTenantEntity<Session>(modelBuilder);
        ConfigureTenantEntity<SessionOccurrence>(modelBuilder);
        ConfigureTenantEntity<Reservation>(modelBuilder);
        ConfigureTenantEntity<CapacityPolicy>(modelBuilder);
        ConfigureTenantEntity<BookingPolicy>(modelBuilder);
        ConfigureTenantEntity<SessionReminderPolicy>(modelBuilder);
        ConfigureTenantEntity<AppointmentAuditEntry>(modelBuilder);

        _ = modelBuilder.Entity<SessionOccurrence>(entity =>
        {
            _ = entity.HasOne(o => o.Session)
                .WithMany()
                .HasForeignKey(o => o.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = entity.Ignore(e => e.Stats);
        });

        _ = modelBuilder.Entity<Reservation>(entity =>
        {
            _ = entity.HasOne(r => r.Occurrence)
                .WithMany()
                .HasForeignKey(r => r.SessionOccurrenceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        _ = modelBuilder.Entity<CapacityPolicy>(entity =>
        {
            _ = entity.HasIndex(p => p.SessionId).IsUnique();
        });

        _ = modelBuilder.Entity<BookingPolicy>(entity =>
        {
            _ = entity.HasIndex(p => p.SessionId).IsUnique();
        });

        _ = modelBuilder.Entity<SessionReminderPolicy>(entity =>
        {
            _ = entity.HasOne<Session>()
                .WithMany()
                .HasForeignKey(p => p.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = entity.HasIndex(p => new { p.SessionId, p.Kind, p.OffsetBeforeOccurrenceStart }).IsUnique();

            var statusesProperty = entity.Property(p => p.QualifyingReservationStatuses)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<HashSet<ReservationStatus>>(v, (JsonSerializerOptions)null) ?? new HashSet<ReservationStatus>());

            statusesProperty.Metadata.SetValueComparer(new ValueComparer<HashSet<ReservationStatus>>(
                (left, right) => left != null && right != null && left.SetEquals(right),
                statuses => statuses.Aggregate(0, (hash, status) => hash ^ status.GetHashCode()),
                statuses => statuses.ToHashSet()));
        });

        _ = modelBuilder.Entity<Session>(entity =>
        {
            _ = entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null))
                .HasDefaultValueSql("'{}'")
                .HasColumnType("nvarchar(max)");

            _ = entity.Ignore(e => e.Stats);
        });

        if (Database.IsSqlite())
        {
            _ = modelBuilder.ConfigureEntityDateTimeOffsetConversions();
        }
    }

    private static void ConfigureTenantEntity<TEntity>(ModelBuilder modelBuilder)
        where TEntity : Entity
    {
        EntityTypeBuilder<TEntity> entityBuilder = modelBuilder.Entity<TEntity>();

        _ = entityBuilder.HasQueryFilter(e =>
            EF.Property<DateTimeOffset?>(e, "DeletedAt") == null);

        _ = entityBuilder.IsMultiTenant();
    }
}
