using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Module.DependencyInjection;
using Vitarish.Users.Api;
using Vitarish.Modules.Base.Core;
using Vitarish.Users.Infrastructure.Extensions;

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
        await app.InitializeUsers();
    }

    public override void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapUsersApi();
        _ = endpoints.MapWebAuthnManagementEndpoints();
    }
}
