using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Module.DependencyInjection;
using Vitarish.Users.Api;
using Vitarish.Modules.Base.Core;

namespace Vitarish.Users.Module;

/// <summary>
/// Module composition root for Users registration and initialization.
/// </summary>
public sealed class UsersModule : Modules.Base.Core.Module
{
    public override string Name => "Users";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        builder.AddUsers(configureDbContext ?? (_ => { }));

        return Task.CompletedTask;
    }

    public override async Task Initialize(WebApplication app)
    {
        _ = await app.MigrateDatabaseAsync<UsersDbContext>();
        if (!string.Equals(
                Environment.GetEnvironmentVariable("VITARISH_OPENAPI_GENERATION"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            await app.InitializeUsers();
        }
        _ = app.MapUsersApi();
    }
}
