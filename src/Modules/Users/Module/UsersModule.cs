using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Vitarish.Modules.Base;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Module.DependencyInjection;

namespace Vitarish.Users.Module;

/// <summary>
/// Module composition root for Users registration and initialization.
/// </summary>
public sealed class UsersModule : Vitarish.Modules.Base.Module
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
        await app.InitializeUsers();
    }
}
