using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Vitarish.Links.Domain.Entities;
using Vitarish.Links.Infrastructure.Data;

namespace Links.Tests;

public sealed class LinksDbContextModelTests
{
    [Fact]
    public void SlugProperty_UsesPostgresAvailableCaseSensitiveCollation()
    {
        DbContextOptions<LinksDbContext> options = new DbContextOptionsBuilder<LinksDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using LinksDbContext context = new(options, Array.Empty<IInterceptor>());

        IReadOnlyProperty slugProperty = context.GetService<IDesignTimeModel>()
            .Model
            .FindEntityType(typeof(Link))!
            .FindProperty(nameof(Link.Slug))!;

        Assert.Equal("C", slugProperty.GetCollation());
    }
}
