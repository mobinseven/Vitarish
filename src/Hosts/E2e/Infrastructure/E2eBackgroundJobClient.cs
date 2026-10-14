using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Sea.Infrastructure.BackgroundJobs;

namespace Vitarish.E2e.Infrastructure;

internal sealed class E2eBackgroundJobClient(IHttpContextAccessor httpContextAccessor) : IBackgroundJobClient
{
    private const string UnavailableMessage = "Background jobs are unavailable in the E2E host.";

    public string Create(Job job, IState state)
    {
        if (job.Type != typeof(ProjectObjectRefreshProcessor)
            || job.Method.Name != nameof(ProjectObjectRefreshProcessor.ProcessAsync)
            || job.Args.Count == 0
            || job.Args[0] is not ProjectObjectRefreshRequest)
        {
            throw new InvalidOperationException(UnavailableMessage);
        }

        IServiceProvider services = httpContextAccessor.HttpContext?.RequestServices
            ?? throw new InvalidOperationException(UnavailableMessage);

        ProjectObjectRefreshProcessor processor = services.GetRequiredService<ProjectObjectRefreshProcessor>();
        processor.ProcessAsync((ProjectObjectRefreshRequest)job.Args[0]!, CancellationToken.None).GetAwaiter().GetResult();
        return Guid.CreateVersion7().ToString("N");
    }

    public bool ChangeState(string jobId, IState state, string? expectedState)
        => throw new InvalidOperationException(UnavailableMessage);
}
