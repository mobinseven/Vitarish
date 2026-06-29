using Vitarish.Sea.Application.Queries.Statistics;

namespace Vitarish.Sea.Infrastructure.Services;

[RegisterTransient]
internal class DatabaseStatisticsService(IMediator mediator) : IDatabaseStatisticsService
{
    public async Task<DatabaseStatisticsReadDto> GetAsync(CancellationToken cancellationToken)
    {
        DatabaseStatistics statistics = await mediator.Send(new GetDatabaseStatisticsQuery(), cancellationToken);
        return new DatabaseStatisticsReadDto
        {
            Projects = statistics.Projects,
            Procedures = statistics.Procedures,
            Events = statistics.Events,
            Roles = statistics.Roles,
            Attachments = statistics.Attachments,
            Reminders = statistics.Reminders
        };
    }
}
