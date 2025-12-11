using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Instagram.Scraper;
using Vitarish.Modules.Base;

namespace Vitarish.Instagram.Hosting;

public sealed class InstagramModule : ModuleBase
{
    public override string Name => "Instagram";

    protected override void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configureDbContext)
    {
        services.AddInstagramScraper(configuration);
    }

    protected override void ConfigureEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapInstagramEndpoints();
}
