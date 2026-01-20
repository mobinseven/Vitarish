using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Instagram.Scraper;
using Vitarish.Modules.Base;

namespace Vitarish.Instagram.Interface;

public sealed class InstagramModule : ModuleBase
{
    public override string Name => "Instagram";

    protected override void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration) => services.AddInstagramScraper(configuration);
}
