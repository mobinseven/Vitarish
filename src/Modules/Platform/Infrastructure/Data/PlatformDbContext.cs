using Microsoft.EntityFrameworkCore;
using Vitarish.Entities.Server.Infrastructure;
using Vitarish.Platform.Domain.Entities;

namespace Vitarish.Platform.Infrastructure.Data;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<Theme> Themes => Set<Theme>();
    public DbSet<ThemeSelection> ThemeSelections => Set<ThemeSelection>();
    public DbSet<ConnectionMemorySample> ConnectionMemorySamples => Set<ConnectionMemorySample>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        _ = modelBuilder.ConfigureUtcDateTimeColumns();
        _ = modelBuilder.HasDefaultSchema(nameof(Platform));

        _ = modelBuilder.Entity<Theme>(entity =>
        {
            _ = entity.HasKey(e => e.Id);
            _ = entity.Property(e => e.Name).IsRequired().HasMaxLength(128);
            _ = entity.HasIndex(e => e.Name).IsUnique();

            _ = entity.ComplexProperty(e => e.LightPalette, palette => ConfigurePalette(palette, nameof(Theme.LightPalette)));
            _ = entity.ComplexProperty(e => e.DarkPalette, palette => ConfigurePalette(palette, nameof(Theme.DarkPalette)));
        });

        _ = modelBuilder.Entity<ThemeSelection>(entity =>
        {
            _ = entity.HasKey(e => e.Id);
            _ = entity.Property(e => e.Id).ValueGeneratedNever();
            _ = entity.Property(e => e.ActiveThemeId).IsRequired();
            _ = entity.Property(e => e.UpdatedAt).IsRequired();
            _ = entity.HasOne<Theme>()
                .WithMany()
                .HasForeignKey(e => e.ActiveThemeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        _ = modelBuilder.Entity<ConnectionMemorySample>(entity =>
        {
            _ = entity.HasKey(e => e.Id);
            _ = entity.Property(e => e.CircuitId).IsRequired().HasMaxLength(128);
            _ = entity.Property(e => e.UserId).HasMaxLength(128);
            _ = entity.Property(e => e.TenantId).HasMaxLength(128);
            _ = entity.Property(e => e.ClientId).HasMaxLength(128);
            _ = entity.Property(e => e.ObservedAtUtc).IsRequired();
            _ = entity.Property(e => e.Kind).IsRequired();
            _ = entity.Property(e => e.ProcessPrivateMemoryBytes).IsRequired();
            _ = entity.Property(e => e.ProcessWorkingSetBytes).IsRequired();
            _ = entity.Property(e => e.ManagedHeapBytes).IsRequired();
            _ = entity.Property(e => e.FailureReason).HasMaxLength(ConnectionMemorySample.FailureReasonMaxLength);
            _ = entity.HasIndex(e => e.CircuitId);
            _ = entity.HasIndex(e => e.ObservedAtUtc);
        });
    }

    private static void ConfigurePalette(Microsoft.EntityFrameworkCore.Metadata.Builders.ComplexPropertyBuilder palette, string columnName)
        => palette.ToJson(columnName);
}
