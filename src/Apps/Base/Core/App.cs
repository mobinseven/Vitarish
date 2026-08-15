using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

namespace Vitarish.Apps.Base.Core;
/// <summary>
/// App is a package supporting a certain functionality.
/// </summary>
public abstract class App : IApp
{
    public abstract string Name { get; }

    public abstract Task Initialize(WebApplication app);
    public abstract Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null);
    public abstract void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints);
}

public interface IApp
{
    string Name { get; }
    Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null);
    void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints);
    Task Initialize(WebApplication app);
}
