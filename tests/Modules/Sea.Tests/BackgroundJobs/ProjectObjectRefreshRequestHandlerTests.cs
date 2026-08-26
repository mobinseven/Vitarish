using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Moq;
using Vitarish.Sea.Infrastructure.BackgroundJobs;

namespace Vitarish.Sea.Tests.BackgroundJobs;

public sealed class ProjectObjectRefreshRequestHandlerTests
{
    [Fact]
    public async Task HandleAsync_enqueues_a_background_refresh()
    {
        Mock<IBackgroundJobClient> jobs = new(MockBehavior.Strict);
        jobs.Setup(client => client.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns("job-id");
        ProjectObjectRefreshRequestHandler handler = new(jobs.Object);

        await handler.HandleAsync(new("tenant", "patient.surgery"));

        jobs.Verify(client => client.Create(It.Is<Job>(job => job.Type == typeof(ProjectObjectRefreshProcessor)), It.IsAny<IState>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_surfaces_enqueue_failure_without_refreshing_inline()
    {
        Mock<IBackgroundJobClient> jobs = new(MockBehavior.Strict);
        jobs.Setup(client => client.Create(It.IsAny<Job>(), It.IsAny<IState>())).Throws(new InvalidOperationException("Hangfire is unavailable."));
        ProjectObjectRefreshRequestHandler handler = new(jobs.Object);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(new("tenant", "patient.surgery")));

        Assert.Equal("The project-object materialized-view refresh could not be queued.", exception.Message);
    }
}
