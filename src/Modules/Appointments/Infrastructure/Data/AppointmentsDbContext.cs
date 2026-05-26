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
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<AppointmentServiceProvider> ServiceProviders => Set<AppointmentServiceProvider>();
    public DbSet<ShiftBookingPolicy> ShiftBookingPolicies => Set<ShiftBookingPolicy>();
    public DbSet<ShiftReminderPolicy> ShiftReminderPolicies => Set<ShiftReminderPolicy>();

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

        modelBuilder.HasDefaultSchema("Appointments");

        _ = modelBuilder.ConfigureMultiTenant();

        ConfigureTenantEntity<Shift>(modelBuilder);
        ConfigureTenantEntity<Reservation>(modelBuilder);
        ConfigureTenantEntity<ServiceType>(modelBuilder);
        ConfigureTenantEntity<Service>(modelBuilder);
        ConfigureTenantEntity<AppointmentServiceProvider>(modelBuilder);
        ConfigureTenantEntity<ShiftBookingPolicy>(modelBuilder);
        ConfigureTenantEntity<ShiftReminderPolicy>(modelBuilder);

        _ = modelBuilder.Entity<Shift>(entity =>
        {
            _ = entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null))
                .HasDefaultValueSql("'{}'")
                .HasColumnType("nvarchar(max)");

            _ = entity.HasOne<AppointmentServiceProvider>()
                .WithMany()
                .HasForeignKey(s => s.ServiceProviderId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            _ = entity.HasOne<Service>()
                .WithMany()
                .HasForeignKey(s => s.ServiceId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            _ = entity.HasIndex(s => s.ServiceProviderId)
                .HasDatabaseName("IX_Shifts_ServiceProviderId");

            _ = entity.HasIndex(s => s.ServiceId)
                .HasDatabaseName("IX_Shifts_ServiceId");
        });

        _ = modelBuilder.Entity<ShiftBookingPolicy>(entity =>
        {
            entity.ToTable("ShiftBookingPolicies");

            _ = entity.HasOne<Shift>()
                .WithMany()
                .HasForeignKey(p => p.ShiftId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = entity.HasIndex(p => p.ShiftId)
                .HasDatabaseName("IX_ShiftBookingPolicies_ShiftId");

            _ = entity.HasIndex(p => new { p.ShiftId, p.Date })
                .HasDatabaseName("IX_ShiftBookingPolicies_ShiftId_Date");
        });

        _ = modelBuilder.Entity<ShiftReminderPolicy>(entity =>
        {
            entity.ToTable("ShiftReminderPolicies");

            _ = entity.HasOne<Shift>()
                .WithMany()
                .HasForeignKey(p => p.ShiftId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = entity.Property(p => p.QualifyingStatuses)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<ReservationStatus>>(v, (JsonSerializerOptions?)null)
                         ?? new List<ReservationStatus>())
                .HasColumnType("nvarchar(max)");

            _ = entity.HasIndex(p => p.ShiftId)
                .HasDatabaseName("IX_ShiftReminderPolicies_ShiftId");
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

        _ = modelBuilder.Entity<OpenBookingShiftDate>(entity =>
        {
            entity.ToView("vw_OpenBookingShiftDates");
            entity.HasNoKey();
            entity.IsMultiTenant();
        });

        _ = modelBuilder.Entity<Service>(entity =>
        {
            _ = entity.HasOne(s => s.ServiceType)
                .WithMany()
                .HasForeignKey(s => s.ServiceTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        _ = modelBuilder.Entity<AppointmentServiceProvider>(entity =>
        {
            _ = entity.HasMany(sp => sp.UserLinks)
                .WithOne()
                .HasForeignKey(l => l.ServiceProviderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        _ = modelBuilder.Entity<ServiceProviderUserLink>(entity =>
        {
            entity.ToTable("ServiceProviderUserLinks");
            entity.HasKey(l => new { l.ServiceProviderId, l.UserId });
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
