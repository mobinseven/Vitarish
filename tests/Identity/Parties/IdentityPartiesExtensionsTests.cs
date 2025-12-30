namespace Vitarish.Parties.Tests;

using DnsClient;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Hosting.DependencyInjection;
using Vitarish.Parties.Infrastructure.Abstractions;
using Vitarish.Parties.Infrastructure.Data;

public class IdentityPartiesExtensionsTests
{
    [Fact]
    public void AddAaasParties_RegistersServices()
    {
        var builder = WebApplication.CreateBuilder();

        builder.AddAaasParties(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        using var provider = builder.Services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<PartiesDbContext>());
        Assert.NotNull(provider.GetService<IPartiesService>());
        Assert.NotNull(provider.GetService<ITenantsService>());
        Assert.NotNull(provider.GetService<ILookupClient>());
        Assert.NotNull(provider.GetService<IDnsVerificationService>());
        Assert.NotNull(provider.GetService<ISender>());
        Assert.NotNull(provider.GetService<IValidator<CreateTenant>>());
    }
}
