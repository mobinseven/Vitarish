using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

namespace Vitarish.Apps.Base;
/// <summary>
/// App is a package supporting a certain functionality.
/// </summary>
public abstract class App : IApp
{
    public abstract string Name { get; }

    public abstract Task Initialize(WebApplication app);
    public abstract Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null);
}

public interface IApp
{
    string Name { get; }
    Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null);
    Task Initialize(WebApplication app);
}
