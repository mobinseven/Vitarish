namespace Vorn.Aaas.RequestIdentities.Infrastructure.Extensions;

using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vorn.Aaas.RequestIdentities.Infrastructure.Data;

public static class RequestIdentitiesWebApplicationBuilderExtensions
{
    public static void AddAaasRequestIdentities(
        this WebApplicationBuilder builder,
        Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        builder.Services.AddDbContext<RequestIdentityDbContext>(dbContextOptionsBuilder, ServiceLifetime.Transient);
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RequestIdentitiesWebApplicationBuilderExtensions).Assembly));
        builder.Services.AddValidatorsFromAssemblyContaining(typeof(RequestIdentitiesWebApplicationBuilderExtensions));
    }
}
