using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Base.Infrastructure.Pipeline;
using Vitarish.Identity.Core.Users.Application.Commands;
using Vitarish.Identity.Infrastructure.Users.Data;
using Vitarish.Identity.Infrastructure.Users.Handlers;
using Vitarish.Identity.Infrastructure.Users.Services;

namespace Vitarish.Identity.Infrastructure.Users.DependencyInjection;

/// <summary>
/// Service registration helpers for user features.
/// </summary>
public static class UsersWebApplicationBuilderExtensions
{
    public static void AddIdentityUsers(this WebApplicationBuilder builder, Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        _ = builder.Services.AddDbContext<UsersDbContext>(dbContextOptionsBuilder);

        _ = builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly));
        _ = builder.Services.AddValidatorsFromAssemblyContaining(typeof(CreateUserWithEmail));

        _ = builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        _ = builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        _ = builder.Services.AddTransient<IUsersService, UsersService>();
        _ = builder.Services.AddTransient<IUserAccessService, UserAccessService>();
        _ = builder.Services.AddSingleton<IUserAccessAuditLogger, UserAccessAuditLogger>();
    }
}
