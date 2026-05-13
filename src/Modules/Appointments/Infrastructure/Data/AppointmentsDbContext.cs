using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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

    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ShiftReservationLock> ShiftReservationLocks => Set<ShiftReservationLock>();

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

        ConfigureTenantEntity<Shift>(modelBuilder);
        ConfigureTenantEntity<Reservation>(modelBuilder);

        _ = modelBuilder.Entity<Shift>(entity =>
        {
            _ = entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null))
                .HasDefaultValueSql("'{}'")
                .HasColumnType("nvarchar(max)");

            _ = entity.Ignore(e => e.Stats);

            _ = entity.OwnsOne(s => s.BookingPolicy, b =>
            {
                b.ToTable("ShiftBookingPolicies");
                b.Property(p => p.ClosedDates)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<ClosedDate>>(v, (JsonSerializerOptions?)null) ?? new List<ClosedDate>())
                    .HasColumnType("nvarchar(max)");
            });

            _ = entity.OwnsMany(s => s.ReminderPolicies, b =>
            {
                b.ToTable("ShiftReminderPolicies");
                b.WithOwner().HasForeignKey("ShiftId");
                b.HasKey("Id");
                b.Property(p => p.QualifyingStatuses)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<ReservationStatus>>(v, (JsonSerializerOptions?)null)
                             ?? new List<ReservationStatus>())
                    .HasColumnType("nvarchar(max)");
                b.HasIndex("ShiftId");
            });
        });

        _ = modelBuilder.Entity<Reservation>(entity =>
        {
            _ = entity.HasOne<Shift>()
                .WithMany()
                .HasForeignKey(r => r.ShiftId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = entity.HasIndex(r => new { r.ShiftId, r.Date })
                .HasDatabaseName("IX_Reservations_ShiftId_Date");
        });

        _ = modelBuilder.Entity<ShiftReservationLock>(entity =>
        {
            entity.ToTable("ShiftReservationLocks");
            entity.HasKey(l => new { l.TenantId, l.ShiftId, l.Date });
        });

        _ = modelBuilder.Entity<ShiftReservation>(entity =>
        {
            entity.ToView("vw_ShiftReservation");
            entity.HasNoKey();
        });

        _ = modelBuilder.Entity<ShiftStats>(eb =>
        {
            _ = eb.ToView("vw_ShiftStats");
            _ = eb.HasNoKey();
        });
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
