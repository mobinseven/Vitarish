using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vitarish.Entities.Server.Infrastructure;
using Vitarish.Platform.Domain.Entities;
using Vitarish.Platform.Domain.Models;

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
            _ = entity.Property(e => e.CreatedAt).IsRequired();
            _ = entity.Property(e => e.UpdatedAt).IsRequired();
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

    private static void ConfigurePalette(ComplexPropertyBuilder<ThemePalette> palette, string columnName)
    {
        _ = palette.ToJson(columnName);
        _ = palette.Property(e => e.Primary).IsRequired().HasMaxLength(7);
        _ = palette.Property(e => e.PrimaryContrastText).IsRequired().HasMaxLength(7);
        _ = palette.Property(e => e.Secondary).IsRequired().HasMaxLength(7);
        _ = palette.Property(e => e.SecondaryContrastText).IsRequired().HasMaxLength(7);
        _ = palette.Property(e => e.Background).IsRequired().HasMaxLength(7);
        _ = palette.Property(e => e.Surface).IsRequired().HasMaxLength(7);
        _ = palette.Property(e => e.AppbarBackground).IsRequired().HasMaxLength(7);
        _ = palette.Property(e => e.AppbarText).IsRequired().HasMaxLength(7);
        _ = palette.Property(e => e.TextPrimary).IsRequired().HasMaxLength(7);
        _ = palette.Property(e => e.TextSecondary).IsRequired().HasMaxLength(7);
        _ = palette.Property(e => e.Divider).IsRequired().HasMaxLength(7);
    }
}
