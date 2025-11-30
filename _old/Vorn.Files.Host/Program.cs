using Microsoft.Extensions.Options;
using Vorn.Files.Host.Domain.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

await builder.AddFilesHost();

WebApplication app = builder.Build();

VornOptions options = app.Services.GetRequiredService<IOptions<VornOptions>>().Value;

await app.UseFilesHost();

app.MapGet("/", () => "Vorn.Files is running.");

app.Run();
