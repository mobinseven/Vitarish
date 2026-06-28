using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Vitarish.Authentication.Module.DependencyInjection;
using Vitarish.Modules.Base.Core;

namespace Vitarish.Authentication.Module;

public sealed class AuthenticationModule : Modules.Base.Core.Module
{
    public override string Name => "Authentication";
    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        builder.Services.AddAuthenticationModuleServices();
        return Task.CompletedTask;
    }
    public override Task Initialize(WebApplication app)
    {
        app.MapAuthenticationModuleEndpoints();
        return Task.CompletedTask;
    }
}
