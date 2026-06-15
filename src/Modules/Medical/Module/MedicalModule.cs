using Vitarish.Medical.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Medical.Infrastructure.Data;
using Vitarish.Medical.Infrastructure.DependencyInjection;
using Vitarish.Modules.Base;
using Vitarish.Entities.Server.Extensions;

namespace Vitarish.Medical.Module;

public sealed class MedicalModule : Vitarish.Modules.Base.Module, IMedicalModule
{
    public override string Name => "Medical";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddSingleton<IMedicalModule, MedicalModule>();
        _ = builder.Services.AddTransientDbContext<MedicalDbContext>((_, options) => configureDbContext?.Invoke(options));
        _ = builder.Services.AddMedicalInfrastructure(builder.Configuration);

        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app) => Task.CompletedTask;
}
