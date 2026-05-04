using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Calendar.Application.Abstractions;
using Vitarish.Calendar.Infrastructure.DependencyInjection;
using Vitarish.Modules.Base;

namespace Vitarish.Calendar.Hosting;

public class CalendarModule : Module, ICalendarModule
{
    public override string Name => "Calendar";

    public override Task Initialize(WebApplication app) => Task.CompletedTask;

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        IConfiguration configuration = builder.Configuration;

        _ = builder.Services.AddCalendarInfrastructure(configuration, configureDbContext);
        _ = builder.Services.AddSingleton<ICalendarModule, CalendarModule>();

        return Task.CompletedTask;
    }
}
