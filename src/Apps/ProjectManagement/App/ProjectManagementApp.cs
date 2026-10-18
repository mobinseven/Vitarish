using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Apps.Base.Core;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.ProjectManagement.Api;
using Vitarish.ProjectManagement.Application;
using Vitarish.ProjectManagement.Infrastructure;
using Vitarish.Intelligence.Domain.Options;

namespace Vitarish.ProjectManagement.App;

public sealed class ProjectManagementApp : Vitarish.Apps.Base.Core.App
{
    public override string Name => "ProjectManagement";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(RunProjectManagementAgentHandler).Assembly));
        _ = builder.Services.AddSingleton<ProjectManagementPresentationBuilder>();
        _ = builder.Services.AddSingleton(new ProjectManagementAgentOptions(
            builder.Configuration[AgentRuntimeOptions.ModelConfigurationKey]
            ?? builder.Configuration[AiOptions.DefaultResponseModelConfigurationKey]
            ?? throw new InvalidOperationException($"{AiOptions.DefaultResponseModelConfigurationKey} must be configured.")));
        _ = builder.Services.AddTransient<ProjectManagementAgentConversationEndpoint>();
        _ = builder.Services.AddTransient<IAgentConversationEndpoint>(sp => sp.GetRequiredService<ProjectManagementAgentConversationEndpoint>());
        _ = builder.Services.AddTransient<ProjectManagementRuntimeToolEndpoint>();
        _ = builder.Services.AddTransient<IAgentRuntimeToolEndpoint>(sp => sp.GetRequiredService<ProjectManagementRuntimeToolEndpoint>());
        _ = builder.Services.AddSingleton<IProjectManagementActionStore, RedisProjectManagementActionStore>();
        _ = builder.Services.AddTransient<ProjectManagementActionWorkflows>();
        _ = builder.Services.AddSingleton(TimeProvider.System);
        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app)
    {
        return Task.CompletedTask;
    }

    public override void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) => endpoints.MapProjectManagementApi();
}
