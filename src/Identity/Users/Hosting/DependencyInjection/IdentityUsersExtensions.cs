using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Domain.Options;
using Vitarish.Users.Infrastructure;
using Vitarish.Users.Infrastructure.Abstractions;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Extensions;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Hosting.DependencyInjection;

public static class IdentityUsersExtensions
{
    public static void AddUsers(
        this WebApplicationBuilder builder,
        Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        _ = builder.Services.AddDbContext<UsersDbContext>(dbContextOptionsBuilder, ServiceLifetime.Transient);

        _ = builder.Services
            .AddIdentity<User, IdentityRole>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<UsersDbContext>()
            .AddSignInManager<AuditingSignInManager>()
            .AddClaimsPrincipalFactory<UserInfoClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();

        _ = builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly));
        _ = builder.Services.AddValidatorsFromAssemblyContaining(typeof(AssemblyMarker));
        _ = builder.Services.Configure<PersonalAccessTokenOptions>(builder.Configuration.GetSection("PersonalAccessTokens"));
        _ = builder.Services.Configure<TemporaryAutoAuthLinkOptions>(
            builder.Configuration.GetSection("Vitarish").GetSection(TemporaryAutoAuthLinkOptions.SectionName));
        _ = builder.Services.AddSingleton<IUserAccessAuditLogger, UserAccessAuditLogger>();
        _ = builder.Services.AddTransient<IUserProfileService, UserProfileService>();
        _ = builder.Services.AddTransient<IPersonalAccessTokenGenerator, PersonalAccessTokenGenerator>();
        builder.Services.TryAddSingleton(TimeProvider.System);
        _ = builder.Services.AddTransient<ITemporaryAutoAuthLinkIssuer, TemporaryAutoAuthLinkIssuer>();
        _ = builder.Services.AddTransient<ITemporaryAutoAuthLinkRedeemer, TemporaryAutoAuthLinkRedeemer>();
        _ = builder.Services.AddTransient<ITemporaryAutoAuthLinkCleanup, TemporaryAutoAuthLinkCleanup>();
        _ = builder.Services.AddTransient<IUsersService, UsersService>();
        _ = builder.Services.AddTransient<IWebAuthnService, WebAuthnService>();
    }

    /// <summary>
    /// Maps Users module endpoints.
    /// </summary>
    public static async Task InitializeUsers(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var rm = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var roleExists = await rm.RoleExistsAsync(IdentityDefinitions.PlatformAdmin);
        if (!roleExists)
        {
            await rm.CreateAsync(new IdentityRole(IdentityDefinitions.PlatformAdmin));
        }
        _ = app.MapWebAuthnManagementEndpoints();
    }
}
