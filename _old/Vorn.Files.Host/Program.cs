using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vorn.Files.Host;
using Vorn.Files.Host.Domain.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddFilesHost();

WebApplication app = builder.Build();

VornOptions options = app.Services.GetRequiredService<IOptions<VornOptions>>().Value;

await app.UseFilesHost();

app.Run();
