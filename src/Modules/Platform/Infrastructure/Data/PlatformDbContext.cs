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
            _ = entity.Property(e => e.CreatedAt).IsRequired();
            _ = entity.Property(e => e.UpdatedAt).IsRequired();
            _ = entity.HasIndex(e => e.Name).IsUnique();

            entity.OwnsOne(e => e.LightPalette, ConfigureLightPalette);
            entity.OwnsOne(e => e.DarkPalette, ConfigureDarkPalette);
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

    private static void ConfigureLightPalette(Microsoft.EntityFrameworkCore.Metadata.Builders.OwnedNavigationBuilder<Theme, ThemePalette> palette)
        => ConfigurePalette(palette, "Light");

    private static void ConfigureDarkPalette(Microsoft.EntityFrameworkCore.Metadata.Builders.OwnedNavigationBuilder<Theme, ThemePalette> palette)
        => ConfigurePalette(palette, "Dark");

    private static void ConfigurePalette(Microsoft.EntityFrameworkCore.Metadata.Builders.OwnedNavigationBuilder<Theme, ThemePalette> palette, string prefix)
    {
        _ = palette.Property(e => e.Primary).IsRequired().HasMaxLength(7).HasColumnName($"{prefix}Primary");
        _ = palette.Property(e => e.PrimaryContrastText).IsRequired().HasMaxLength(7).HasColumnName($"{prefix}PrimaryContrastText");
        _ = palette.Property(e => e.Secondary).IsRequired().HasMaxLength(7).HasColumnName($"{prefix}Secondary");
        _ = palette.Property(e => e.SecondaryContrastText).IsRequired().HasMaxLength(7).HasColumnName($"{prefix}SecondaryContrastText");
        _ = palette.Property(e => e.Background).IsRequired().HasMaxLength(7).HasColumnName($"{prefix}Background");
        _ = palette.Property(e => e.Surface).IsRequired().HasMaxLength(7).HasColumnName($"{prefix}Surface");
        _ = palette.Property(e => e.AppbarBackground).IsRequired().HasMaxLength(7).HasColumnName($"{prefix}AppbarBackground");
        _ = palette.Property(e => e.AppbarText).IsRequired().HasMaxLength(7).HasColumnName($"{prefix}AppbarText");
        _ = palette.Property(e => e.TextPrimary).IsRequired().HasMaxLength(7).HasColumnName($"{prefix}TextPrimary");
        _ = palette.Property(e => e.TextSecondary).IsRequired().HasMaxLength(7).HasColumnName($"{prefix}TextSecondary");
        _ = palette.Property(e => e.Divider).IsRequired().HasMaxLength(7).HasColumnName($"{prefix}Divider");
    }
}
