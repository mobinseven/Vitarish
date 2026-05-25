using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Medical.Application.Abstractions;
using Vitarish.Medical.Infrastructure.Data;
using Vitarish.Medical.Infrastructure.DependencyInjection;
using Vitarish.Modules.Base;

namespace Vitarish.Medical.Interface;

public sealed class MedicalModule : Module, IMedicalModule
{
    public override string Name => "Medical";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddSingleton<IMedicalModule, MedicalModule>();
        _ = builder.Services.AddDbContext<MedicalDbContext>((_, options) => configureDbContext?.Invoke(options), ServiceLifetime.Transient);
        _ = builder.Services.AddMedicalInfrastructure();

        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app) => Task.CompletedTask;
}
