using Microsoft.EntityFrameworkCore;
using Vitarish.Entities.Server.Infrastructure;
using Vitarish.Platform.Domain.Entities;

namespace Vitarish.Platform.Infrastructure.Data;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<Theme> Themes => Set<Theme>();
    public DbSet<ThemeSelection> ThemeSelections => Set<ThemeSelection>();

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
    }

    private static void ConfigurePalette(Microsoft.EntityFrameworkCore.Metadata.Builders.ComplexPropertyBuilder palette, string columnName)
        => palette.ToJson(columnName);
}
