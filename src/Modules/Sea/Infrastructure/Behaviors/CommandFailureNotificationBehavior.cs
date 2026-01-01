namespace Vitarish.Sea.Infrastructure.Behaviors;


internal class CommandFailureNotificationBehavior<TRequest, TResponse>(IPublisher publisher) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            await publisher.Publish(new CommandFailureNotification(typeof(TRequest).Name, ex), cancellationToken);
            throw;
        }
    }
}
