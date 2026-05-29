using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Modules.Base;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Infrastructure.Data;
using Vitarish.Prescription.Infrastructure.DependencyInjection;

namespace Vitarish.Prescription.Interface;

public sealed class PrescriptionModule : Module, IPrescriptionModule
{
    public override string Name => "Prescription";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddSingleton<IPrescriptionModule, PrescriptionModule>();
        _ = builder.Services.AddDbContext<PrescriptionDbContext>((_, options) => configureDbContext?.Invoke(options), ServiceLifetime.Transient);
        _ = builder.Services.AddPrescriptionInfrastructure();

        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app) => Task.CompletedTask;
}
