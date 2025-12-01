using Microsoft.AspNetCore.Authentication.JwtBearer;
using Vitarish.Communications.Api.HealthChecks;
using Vitarish.Communications.Api.Messages.Endpoints;
using Vitarish.Communications.Infrastructure.Messages.DependencyInjection;
using Vitarish.Communications.Infrastructure.Messages.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add Communications messaging services (includes MediatR handlers and health monitoring)
builder.Services.AddCommunicationsMessaging(builder.Configuration);

// Configure JWT authentication
var communicationsOptions = builder.Configuration
    .GetSection(CommunicationsOptions.Section)
    .Get<CommunicationsOptions>();

if (string.IsNullOrWhiteSpace(communicationsOptions?.IdentityAuthority))
{
    throw new InvalidOperationException(
        "IdentityAuthority must be configured in Communications:IdentityAuthority configuration section");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = communicationsOptions.IdentityAuthority;
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters.ValidateAudience = true;
        options.TokenValidationParameters.ValidAudience = "communications-api";
        options.TokenValidationParameters.ValidTypes = ["at+jwt"];
    });

builder.Services.AddAuthorization();

// Add OpenAPI/Swagger support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

// Map API endpoints
app.MapMessagesEndpoints();

// Add health check endpoint
app.MapGet("/", () => "Communications.Api is running.")
    .WithName("HealthCheck");

// Optional: Send startup test SMS
app.UseStartupSmsTest();

app.Run();
