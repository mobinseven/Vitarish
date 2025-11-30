using MediatR;
using Microsoft.Extensions.Logging;

namespace Vitarish.Base.Infrastructure.Pipeline;

/// <summary>
/// Pipeline behavior that logs the start and end of request handling.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Handling {RequestName}", requestName);

        try
        {
            var response = await next();

            logger.LogInformation("Handled {RequestName}", requestName);

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling {RequestName}", requestName);
            throw;
        }
    }
}
