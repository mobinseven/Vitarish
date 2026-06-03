using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Vitarish.Modules.Base;
using Vitarish.Users.Hosting.DependencyInjection;

namespace Vitarish.Users.Hosting;

/// <summary>
/// Module composition root for Users registration and initialization.
/// </summary>
public sealed class UsersModule : Module
{
    public override string Name => "Users";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        builder.AddUsers(configureDbContext ?? (_ => { }));

        return Task.CompletedTask;
    }

    public override async Task Initialize(WebApplication app) => await app.InitializeUsers();
}
