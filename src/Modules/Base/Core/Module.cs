using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Vitarish.Modules.Base.Core;

public abstract class Module : IModule
{
    public const string ApplicationSection = "Vitarish";
    public const string EvaluatedConfigurationSection = ApplicationSection + ":EvaluatedConfiguration";
    public const string EvaluatedConfigurationEnabledKey = EvaluatedConfigurationSection + ":Enabled";
    public const string EvaluatedConfigurationIdentityKey = EvaluatedConfigurationSection + ":Identity";
    public const string MainOnlyEvaluatedProfileIdentity = "vitarish-toe-main-evaluated-2026-08-15";
    public const string ProductEvaluatedProfileIdentity = "vitarish-product-evaluated-v1.0";

    public abstract string Name { get; }

    protected string OptionsSectionPath => $"{ApplicationSection}:{Name}";

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
    public abstract void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints);
    public abstract Task Initialize(WebApplication app);
}

public interface IModule
{
    string Name { get; }
    void RegisterOptions(IServiceCollection services, IConfiguration configuration, string sectionPath);
    Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null);
    void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints);
    Task Initialize(WebApplication app);
    bool IsAvailable { get; set; }
    event Action<bool> AvailabilityChanged;
}
