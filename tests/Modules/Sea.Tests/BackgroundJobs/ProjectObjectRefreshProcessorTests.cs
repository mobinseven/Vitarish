using Microsoft.Extensions.Logging.Abstractions;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Infrastructure.BackgroundJobs;

namespace Vitarish.Sea.Tests.BackgroundJobs;

public sealed class ProjectObjectRefreshProcessorTests
{
    [Fact]
    public async Task ReconcileAsync_refreshes_every_registered_view()
    {
        RecordingRefresher surgery = new("patient.surgery");
        RecordingRefresher other = new("patient.other");
        ProjectObjectRefreshProcessor processor = new([surgery, other], NullLogger<ProjectObjectRefreshProcessor>.Instance);

        await processor.ReconcileAsync();

        Assert.Equal([string.Empty], surgery.Refreshes);
        Assert.Equal([string.Empty], other.Refreshes);
    }

    private sealed class RecordingRefresher(string objectType) : IProjectObjectMaterializedViewRefresher
    {
        public string ObjectType { get; } = objectType;
        public List<string> Refreshes { get; } = [];

        public Task RefreshAsync(string tenantId, CancellationToken cancellationToken)
        {
            Refreshes.Add(tenantId);
            return Task.CompletedTask;
        }
    }
}
