using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

namespace Vitarish.Estates.Base;
/// <summary>
/// Estate is a package that can be assigned to a tenant. The name is chosen as such to resemble real estates being owned by tenants.
/// </summary>
public abstract class Estate : IEstate
{
    public abstract string Name { get; }

    public abstract Task Initialize(WebApplication app);
    public abstract Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null);
}

public interface IEstate
{
    string Name { get; }
    Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null);
    Task Initialize(WebApplication app);
}
