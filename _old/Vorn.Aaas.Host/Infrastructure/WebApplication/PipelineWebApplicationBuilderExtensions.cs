using MediatR;
using Vorn.Aaas.Host.Infrastructure.Pipeline;
using Vorn.Aaas.Users.Infrastructure.Service;

public static class PipelineWebApplicationBuilderExtensions
{
    public static void AddAaasPipeline(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        _ = builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        _ = builder.Services.AddSingleton<EventPublisher>();
    }
}
