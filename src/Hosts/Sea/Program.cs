using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Vitarish.Clients.Interface;
using Vitarish.Clients.Interface.Identity.Extensions;
using Vitarish.Clients.Interface.Identity.Services;
using MudBlazor.Services;
using Vitarish.Sea.Components;
using Vitarish.Sea.Services;
using Vitarish.Sea.Tools;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

_ = builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddHubOptions(options =>
                {
                    options.EnableDetailedErrors = true;
                    options.ClientTimeoutInterval = TimeSpan.FromMinutes(10);
                    options.MaximumParallelInvocationsPerClient = 1;
                    options.MaximumReceiveMessageSize = 128 * 1024;
                });

bool isTesting = builder.Environment.IsEnvironment("Testing");

if (isTesting)
{
    builder.Services.AddLocalization();
    builder.Services.AddAuthorization();
    builder.Services.AddAuthentication();
    builder.Services.AddMudServices();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, BlazorAuthorizationMiddlewareResultHandler>();
    builder.Services.AddScoped<IAuthorizationPolicyProvider, TenantRolePolicyProvider>();
    builder.Services.AddScoped<IAuthorizationHandler, TenantRoleHandler>();
}
else
{
    await builder.Services.AddInterfaceModules(builder.Configuration);

    builder.Services.AddScoped<IMcpToolProvider, McpToolProvider>();

    IMcpServerBuilder mcpServerBuilder = builder.Services.AddMcpServer();
    mcpServerBuilder.WithHttpTransport(o =>
    {
        o.Stateless = true;
    });
    mcpServerBuilder.WithToolsFromAssembly(toolAssembly: typeof(AttachmentsTools).Assembly);
}

WebApplication app = builder.Build();

if (isTesting)
{
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/api/admin", () => Results.Ok(new { ok = true }))
       .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
}
else
{
    app.UseCommonMiddlewares();

    RouteGroupBuilder mcp = (RouteGroupBuilder)app.MapMcp("/mcp").RequireAuthorization(new AuthorizeAttribute
    {
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
    });

    app.MapInterfaceModules();
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program { }
