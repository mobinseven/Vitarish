
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Domain.Options;
using Vitarish.Users.Infrastructure;
using Vitarish.Users.Infrastructure.Abstractions;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Hosting.DependencyInjection;

public static class IdentityUsersExtensions
{
    public static void AddUsers(
        this WebApplicationBuilder builder,
        Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        _ = builder.Services.AddDbContext<UsersDbContext>(dbContextOptionsBuilder);
        _ = builder.Services.AddIdentityCore<User>().AddEntityFrameworkStores<UsersDbContext>();
        _ = builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly));
        _ = builder.Services.AddValidatorsFromAssemblyContaining(typeof(AssemblyMarker));
        _ = builder.Services.Configure<PersonalAccessTokenOptions>(builder.Configuration.GetSection("PersonalAccessTokens"));
        _ = builder.Services.AddSingleton<IUserAccessAuditLogger, UserAccessAuditLogger>();
        _ = builder.Services.AddTransient<IUserProfileService, UserProfileService>();
        _ = builder.Services.AddTransient<IPersonalAccessTokenGenerator, PersonalAccessTokenGenerator>();
        _ = builder.Services.AddTransient<IUsersService, UsersService>();
    }
}
