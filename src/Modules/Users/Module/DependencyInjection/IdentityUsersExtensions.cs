using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Application.ProfileCompletion;
using Vitarish.Users.Contracts.Authorization;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Domain.Options;
using Vitarish.Users.Infrastructure;
using Vitarish.Users.Infrastructure.Abstractions;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Extensions;
using Vitarish.Users.Infrastructure.Service;
using Vitarish.Users.Infrastructure.Authentication;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Entities.Server.Extensions;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Users.Module.DependencyInjection;

public static class IdentityUsersExtensions
{
    public static void AddUsers(
        this WebApplicationBuilder builder,
        Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        _ = builder.Services.AddTransientDbContext<UsersDbContext>((_, options) => dbContextOptionsBuilder(options));

        _ = builder.Services
            .AddIdentity<User, IdentityRole>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<UsersDbContext>()
            .AddSignInManager<AuditingSignInManager>()
            .AddClaimsPrincipalFactory<UserInfoClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();

        _ = builder.Services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequiredLength = PasswordPolicy.Default.MinimumLength;
            options.Password.RequireLowercase = PasswordPolicy.Default.RequireLowercase;
            options.Password.RequireUppercase = PasswordPolicy.Default.RequireUppercase;
            options.Password.RequireDigit = PasswordPolicy.Default.RequireNumber;
            options.Password.RequireNonAlphanumeric = PasswordPolicy.Default.RequireSpecialCharacter;
            options.Lockout.MaxFailedAccessAttempts = SecurityAccessPolicy.Default.FailedAttemptThreshold;
            options.Lockout.DefaultLockoutTimeSpan = SecurityAccessPolicy.Default.FailedAttemptDenialDuration;
            options.Lockout.AllowedForNewUsers = true;
        });

        _ = builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly));
        _ = builder.Services.AddValidatorsFromAssemblyContaining(typeof(AssemblyMarker));
        _ = builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(UsersAuthorizationPolicies.ManageAccounts, policy => policy.RequireRole(IdentityDefinitions.PlatformAdmin));
            options.AddPolicy(UsersAuthorizationPolicies.ReadSecurityState, policy => policy.RequireRole(IdentityDefinitions.PlatformAdmin));
            options.AddPolicy(UsersAuthorizationPolicies.AssignGlobalRole, policy => policy.RequireRole(IdentityDefinitions.PlatformAdmin));
            options.AddPolicy(UsersAuthorizationPolicies.ReadTenantRoles, policy => policy.RequireRole(IdentityDefinitions.PlatformAdmin));
            options.AddPolicy(UsersAuthorizationPolicies.ReplaceTenantRole, policy => policy.RequireRole(IdentityDefinitions.PlatformAdmin));
        });
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
        _ = builder.Services.AddScoped<IWebAuthnCredentialLookup, WebAuthnCredentialLookup>();
        _ = builder.Services.AddScoped<IProfileCompletionPolicy, ProfileCompletionPolicy>();
        _ = builder.Services.AddScoped<UserSubjectAccessPolicy>();
    }

    /// <summary>
    /// Maps Users module endpoints.
    /// </summary>
    public static async Task InitializeUsers(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        RoleManager<IdentityRole> rm = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        bool roleExists = await rm.RoleExistsAsync(IdentityDefinitions.PlatformAdmin);
        if (!roleExists)
        {
            _ = await rm.CreateAsync(new IdentityRole(IdentityDefinitions.PlatformAdmin));
        }
    }
}
