using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Parties.Application;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Application.SignIn;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Services;
using Vitarish.Users.Application.Queries;

namespace Parties.Tests.Infrastructure;

public sealed class AuthenticationHostDirectoryTests
{
    [Theory]
    [InlineData("https://pm.example.test", true)]
    [InlineData("https://pm.example.test:444", false)]
    [InlineData("http://pm.example.test", false)]
    [InlineData("https://user@pm.example.test", false)]
    [InlineData("https://unknown.example.test", false)]
    [InlineData("http://localhost", false)]
    public async Task Registered_origin_is_exact_and_cannot_downgrade_https(string origin, bool expected)
    {
        await using PartiesDbContext database = new(new DbContextOptionsBuilder<PartiesDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            Mock.Of<IPublisher>(), Array.Empty<IInterceptor>());
        Guid tenantId = Guid.NewGuid();
        database.Tenants.Add(new Tenant { Id = tenantId, OwnerId = Guid.NewGuid(), AppId = BrowserApplicationTenants.ProjectManagementAppId, Name = "PM" });
        database.TenantIndicators.Add(new TenantIndicator { Id = Guid.NewGuid(), TenantId = tenantId, Method = IndicatorMethod.Hostname, Value = "pm.example.test", Status = IndicatorStatus.Active, IsPrimary = true });
        database.TenantIndicators.Add(new TenantIndicator { Id = Guid.NewGuid(), TenantId = tenantId, Method = IndicatorMethod.Hostname, Value = "localhost", Status = IndicatorStatus.Active, IsPrimary = false });
        await database.SaveChangesAsync();
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(It.IsAny<GetUserRolesQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(new[] { new IdentityRole(Vitarish.Users.Application.Authorization.TenantRoleDefinitions.BuildRoleName(tenantId, IdentityDefinitions.TenantUser)) });
        AuthenticationHostDirectory hosts = new(database, sender.Object, new HttpContextAccessor(),
            Options.Create(new SignInHostOptions { SignInUrl = "https://auth.example.test" }),
            Options.Create(new OnboardingHostOptions()), Options.Create(new PlatformAdminHostOptions()),
            Options.Create(new BrowserApplicationHostOptions { ProjectManagementUrl = "https://pm.example.test" }), []);
        (await hosts.CanOpenAsync(origin, "administrator", CancellationToken.None)).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("person")]
    public async Task Clinic_landing_host_opens_for_anonymous_and_role_free_people(string? userId)
    {
        await using PartiesDbContext database = new(new DbContextOptionsBuilder<PartiesDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            Mock.Of<IPublisher>(), Array.Empty<IInterceptor>());
        Guid tenantId = Guid.NewGuid();
        database.Tenants.Add(new Tenant { Id = tenantId, OwnerId = Guid.NewGuid(), AppId = BrowserApplicationTenants.ClinicLandingAppId, Name = BrowserApplicationTenants.ClinicLandingName });
        database.TenantIndicators.Add(new TenantIndicator { Id = Guid.NewGuid(), TenantId = tenantId, Method = IndicatorMethod.Hostname, Value = "clinics.example.test", Status = IndicatorStatus.Active, IsPrimary = true });
        await database.SaveChangesAsync();
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(It.IsAny<GetUserRolesQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<IdentityRole>());
        AuthenticationHostDirectory hosts = new(database, sender.Object, new HttpContextAccessor(),
            Options.Create(new SignInHostOptions { SignInUrl = "https://auth.example.test" }),
            Options.Create(new OnboardingHostOptions()), Options.Create(new PlatformAdminHostOptions()),
            Options.Create(new BrowserApplicationHostOptions()), []);
        (await hosts.CanOpenAsync("https://clinics.example.test", userId, CancellationToken.None)).Should().BeTrue();
    }
}
