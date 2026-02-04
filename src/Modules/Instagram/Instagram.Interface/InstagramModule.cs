using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Instagram.Scraper;
using Vitarish.Modules.Base;

namespace Vitarish.Instagram.Interface;

public sealed class InstagramModule : Module
{
    public override string Name => "Instagram";

    public override Task Initialize(WebApplication app) => Task.CompletedTask;
    public override async Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null) => builder.Services.AddInstagramScraper(builder.Configuration);
}
