namespace Vorn.Aaas.Users.Infrastructure.Extensions;

using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vorn.Aaas.Users.Application.Auditing;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Infrastructure.Data;
using Vorn.Aaas.Users.Infrastructure.Logging;
using Vorn.Aaas.Users.Presentation.Service;

public static class UsersWebApplicationBuilderExtensions
{
    public static void AddAaasUsers(
        this WebApplicationBuilder builder,
        Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        builder.Services.AddDbContext<UsersDbContext>(dbContextOptionsBuilder);
        builder.Services.AddIdentityCore<User>().AddEntityFrameworkStores<UsersDbContext>();
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(UsersWebApplicationBuilderExtensions).Assembly));
        builder.Services.AddValidatorsFromAssemblyContaining(typeof(UsersWebApplicationBuilderExtensions));
        builder.Services.Configure<PersonalAccessTokenPreferences>(builder.Configuration.GetSection("PersonalAccessTokens"));
        builder.Services.AddSingleton<IUserAccessAuditLogger, UserAccessAuditLogger>();
        builder.Services.AddTransient<IUserAccessService, UserAccessService>();
        builder.Services.AddTransient<IPersonalAccessTokenGenerator, PersonalAccessTokenGenerator>();
        builder.Services.AddTransient<IUsersService, UsersService>();
    }
}
