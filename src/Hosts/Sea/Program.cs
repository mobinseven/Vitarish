using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Vitarish.Clients.Interface;
using Vitarish.Sea.Components;
using Vitarish.Sea.Services;
using Vitarish.Sea.Tools;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

await builder.Services.AddInterfaceModules(builder.Configuration);

_ = builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddHubOptions(options =>
                {
                    options.EnableDetailedErrors = true;
                    options.ClientTimeoutInterval = TimeSpan.FromMinutes(10);
                    options.MaximumParallelInvocationsPerClient = 1;
                    options.MaximumReceiveMessageSize = 128 * 1024;
                });

builder.Services.AddScoped<IMcpToolProvider, McpToolProvider>();

IMcpServerBuilder mcpServerBuilder = builder.Services.AddMcpServer();
mcpServerBuilder.WithHttpTransport(o =>
{
    o.Stateless = true;
});
mcpServerBuilder.WithToolsFromAssembly(toolAssembly: typeof(AttachmentsTools).Assembly);

WebApplication app = builder.Build();
app.UseCommonMiddlewares();


RouteGroupBuilder mcp = (RouteGroupBuilder)app.MapMcp("/mcp").RequireAuthorization(new AuthorizeAttribute
{
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapInterfaceModules();

app.Run();
