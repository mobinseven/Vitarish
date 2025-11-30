
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Domain.Options;
using Vorn.Aaas.Users.Infrastructure.Abstractions;
using Vorn.Aaas.Users.Infrastructure.Data;
using Vorn.Aaas.Users.Infrastructure.Service;
using Vorn.Aaas.Users.Presentation.Abstractions;

namespace Vorn.Aaas.Users.Presentation.DependencyInjection;

public static class UsersWebApplicationBuilderExtensions
{
    public static void AddAaasUsers(
        this WebApplicationBuilder builder,
        Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        _ = builder.Services.AddDbContext<UsersDbContext>(dbContextOptionsBuilder);
        _ = builder.Services.AddIdentityCore<User>().AddEntityFrameworkStores<UsersDbContext>();
        _ = builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(UsersWebApplicationBuilderExtensions).Assembly));
        _ = builder.Services.AddValidatorsFromAssemblyContaining(typeof(UsersWebApplicationBuilderExtensions));
        _ = builder.Services.Configure<PersonalAccessTokenOptions>(builder.Configuration.GetSection("PersonalAccessTokens"));
        _ = builder.Services.AddSingleton<IUserAccessAuditLogger, UserAccessAuditLogger>();
        _ = builder.Services.AddTransient<IUserAccessService, UserAccessService>();
        _ = builder.Services.AddTransient<IPersonalAccessTokenGenerator, PersonalAccessTokenGenerator>();
        _ = builder.Services.AddTransient<IUsersService, UsersService>();
    }
}
