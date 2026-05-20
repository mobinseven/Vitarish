using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Vitarish.Appointments.Infrastructure.Data;
using Vitarish.Appointments.Infrastructure.Services;
using Vitarish.Requests.Application.Abstractions;

namespace Appointments.Tests;

public class ServiceProviderManagerTests
{
    private static AppointmentsDbContext CreateInMemoryDbContext()
    {
        DbContextOptions<AppointmentsDbContext> options = new DbContextOptionsBuilder<AppointmentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        Mock<IRequestContext> requestContext = new();
        requestContext.Setup(rc => rc.TenantId).Returns("tenant-test");

        AppointmentsDbContext context = new(options, [], requestContext.Object);
        return context;
    }

    private static ServiceProviderManager CreateSut(AppointmentsDbContext dbContext)
    {
        Mock<IMediator> mediator = new();
        return new ServiceProviderManager(mediator.Object, dbContext);
    }

    [Fact]
    public async Task LinkUserAsync_ShouldReturnNotFound_WhenProviderDoesNotExist()
    {
        await using AppointmentsDbContext db = CreateInMemoryDbContext();
        ServiceProviderManager sut = CreateSut(db);

        EntityResult result = await sut.LinkUserAsync(Guid.NewGuid(), "user-1", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "ServiceProviderNotFoundError");
    }

    [Fact]
    public async Task LinkUserAsync_ShouldAddLink_WhenProviderExists()
    {
        await using AppointmentsDbContext db = CreateInMemoryDbContext();
        AppointmentServiceProvider provider = new()
        {
            Id = Guid.NewGuid(),
            Name = "Test Provider",
        };
        db.ServiceProviders.Add(provider);
        await db.SaveChangesAsync();

        ServiceProviderManager sut = CreateSut(db);

        EntityResult result = await sut.LinkUserAsync(provider.Id, "user-1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        AppointmentServiceProvider? updated = await db.ServiceProviders
            .Include(sp => sp.UserLinks)
            .FirstOrDefaultAsync(sp => sp.Id == provider.Id);

        updated.Should().NotBeNull();
        updated!.UserLinks.Should().ContainSingle(l => l.UserId == "user-1");
    }

    [Fact]
    public async Task LinkUserAsync_ShouldBeIdempotent_WhenLinkAlreadyExists()
    {
        await using AppointmentsDbContext db = CreateInMemoryDbContext();
        Guid providerId = Guid.NewGuid();
        AppointmentServiceProvider provider = new()
        {
            Id = providerId,
            Name = "Test Provider",
            UserLinks = [new ServiceProviderUserLink { ServiceProviderId = providerId, UserId = "user-1" }]
        };
        db.ServiceProviders.Add(provider);
        await db.SaveChangesAsync();

        ServiceProviderManager sut = CreateSut(db);

        EntityResult result = await sut.LinkUserAsync(provider.Id, "user-1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        AppointmentServiceProvider? updated = await db.ServiceProviders
            .Include(sp => sp.UserLinks)
            .FirstOrDefaultAsync(sp => sp.Id == provider.Id);

        updated!.UserLinks.Should().HaveCount(1);
    }

    [Fact]
    public async Task UnlinkUserAsync_ShouldReturnNotFound_WhenProviderDoesNotExist()
    {
        await using AppointmentsDbContext db = CreateInMemoryDbContext();
        ServiceProviderManager sut = CreateSut(db);

        EntityResult result = await sut.UnlinkUserAsync(Guid.NewGuid(), "user-1", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "ServiceProviderNotFoundError");
    }

    [Fact]
    public async Task UnlinkUserAsync_ShouldRemoveLink_WhenLinkExists()
    {
        await using AppointmentsDbContext db = CreateInMemoryDbContext();
        Guid providerId = Guid.NewGuid();
        AppointmentServiceProvider provider = new()
        {
            Id = providerId,
            Name = "Test Provider",
            UserLinks = [new ServiceProviderUserLink { ServiceProviderId = providerId, UserId = "user-1" }]
        };
        db.ServiceProviders.Add(provider);
        await db.SaveChangesAsync();

        ServiceProviderManager sut = CreateSut(db);

        EntityResult result = await sut.UnlinkUserAsync(providerId, "user-1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        AppointmentServiceProvider? updated = await db.ServiceProviders
            .Include(sp => sp.UserLinks)
            .FirstOrDefaultAsync(sp => sp.Id == providerId);

        updated!.UserLinks.Should().BeEmpty();
    }

    [Fact]
    public async Task UnlinkUserAsync_ShouldBeIdempotent_WhenLinkDoesNotExist()
    {
        await using AppointmentsDbContext db = CreateInMemoryDbContext();
        AppointmentServiceProvider provider = new()
        {
            Id = Guid.NewGuid(),
            Name = "Test Provider",
        };
        db.ServiceProviders.Add(provider);
        await db.SaveChangesAsync();

        ServiceProviderManager sut = CreateSut(db);

        EntityResult result = await sut.UnlinkUserAsync(provider.Id, "nonexistent-user", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
