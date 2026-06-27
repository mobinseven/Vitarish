using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Financial.Application.Abstractions;
using Vitarish.Financial.Infrastructure.Data;
using Vitarish.Financial.Infrastructure.DependencyInjection;
using Vitarish.Entities.Server.Extensions;
using Vitarish.Modules.Base.Core;

namespace Vitarish.Financial.Module;

public sealed class FinancialModule : Modules.Base.Core.Module, IFinancialModule
{
    public override string Name => "Financial";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddSingleton<IFinancialModule, FinancialModule>();
        _ = builder.Services.AddTransientDbContext<FinancialDbContext>((_, options) => configureDbContext?.Invoke(options));
        _ = builder.Services.AddFinancialInfrastructure();

        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app) => app.MigrateDatabaseAsync<FinancialDbContext>();
}
