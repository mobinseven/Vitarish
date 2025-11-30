
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vorn.Aaas.RequestIdentities.Infrastructure.Data;
using Vorn.Aaas.RequestIdentities.Infrastructure.Services;
using Vorn.Aaas.RequestIdentities.Presentation.Abstractions;

namespace Vorn.Aaas.RequestIdentities.Presentation.DependencyInjection;

public static class RequestIdentitiesWebApplicationBuilderExtensions
{
    public static void AddAaasRequestIdentities(
        this WebApplicationBuilder builder,
        Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        _ = builder.Services.AddDbContext<RequestIdentityDbContext>(dbContextOptionsBuilder, ServiceLifetime.Transient);
        _ = builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RequestIdentitiesWebApplicationBuilderExtensions).Assembly));
        _ = builder.Services.AddValidatorsFromAssemblyContaining(typeof(RequestIdentitiesWebApplicationBuilderExtensions));
        _ = builder.Services.AddTransient<IRequestIdentitiesService, RequestIdentitiesService>();
    }
}
