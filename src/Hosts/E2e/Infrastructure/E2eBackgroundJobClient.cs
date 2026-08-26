using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace Vitarish.E2e.Infrastructure;

internal sealed class E2eBackgroundJobClient : IBackgroundJobClient
{
    public string Create(Job job, IState state)
        => throw new InvalidOperationException("Background jobs are unavailable in the E2E host.");

    public bool ChangeState(string jobId, IState state, string? expectedState)
        => throw new InvalidOperationException("Background jobs are unavailable in the E2E host.");
}
