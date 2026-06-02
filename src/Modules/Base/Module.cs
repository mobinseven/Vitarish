using Microsoft.EntityFrameworkCore;

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
    public abstract Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null);
    public abstract Task Initialize(WebApplication app);
}

public interface IModule
{
    string Name { get; }
    Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null);
    Task Initialize(WebApplication app);
    bool IsAvailable { get; set; }
    event Action<bool> AvailabilityChanged;
}
