using MediatR;

namespace Vorn.Aaas.Host.Services;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
        TResponse? response = await next();
        logger.LogInformation("Handled {Request}", typeof(TRequest).Name);
        return response;
    }
}
