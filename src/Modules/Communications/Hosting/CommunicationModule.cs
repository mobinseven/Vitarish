using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Infrastructure.Services;
using Vitarish.Communications.Infrastructure.Wrappers.SmsDotIr;
using Vitarish.Modules.Base;

namespace Vitarish.Communications.Hosting;
public class CommunicationModule : Module, ICommunicationsModule
{
    public override string Name => "Communications";

    public override Task Initialize(WebApplication app) => Task.CompletedTask;
    public override async Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {

        _ = builder.Services.AddSingleton<ICommunicationsModule, CommunicationModule>();
        _ = builder.Services.AddTransient<IMessagesService, MessagesService>();
        _ = builder.Services.AddTransient<ISmsProvider, SmsDotIrProvider>();
    }
}
