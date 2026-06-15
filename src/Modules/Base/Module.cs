using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Vitarish.Modules.Base;

public abstract class Module : IModule
{
    public abstract string Name { get; }

    public bool IsAvailable
    {
        get;
        set
        {
            if (field != value)
            {
                AvailabilityChanged?.Invoke(value);
            }
            field = value;
        }
    }
    public event Action<bool>? AvailabilityChanged;
    public virtual void RegisterOptions(IServiceCollection services, IConfiguration configuration, string sectionPath)
    {
    }

    public abstract Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null);
    public abstract Task Initialize(WebApplication app);
}

public interface IModule
{
    string Name { get; }
    void RegisterOptions(IServiceCollection services, IConfiguration configuration, string sectionPath);
    Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null);
    Task Initialize(WebApplication app);
    bool IsAvailable { get; set; }
    event Action<bool> AvailabilityChanged;
}
