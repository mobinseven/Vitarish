using Hangfire;

namespace Vitarish.Sea.Infrastructure.BackgroundJobs;

public sealed class ProjectObjectRefreshRequestHandler(
    IBackgroundJobClient backgroundJobClient)
{
    public Task HandleAsync(ProjectObjectRefreshRequest request)
    {
        try
        {
            _ = backgroundJobClient.Enqueue<ProjectObjectRefreshProcessor>(processor =>
                processor.ProcessAsync(request, CancellationToken.None));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidOperationException("The project-object materialized-view refresh could not be queued.", exception);
        }

        return Task.CompletedTask;
    }
}
