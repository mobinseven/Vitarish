using Vitarish.Sea.Application.Queries.Statistics;

namespace Vitarish.Sea.Infrastructure.Handlers;

internal class GetDatabaseStatisticsHandler(SeaDbContext dbContext)
    : IRequestHandler<GetDatabaseStatisticsQuery, DatabaseStatistics>
{
    public async Task<DatabaseStatistics> Handle(GetDatabaseStatisticsQuery request, CancellationToken cancellationToken) => new(
                await dbContext.Projects.CountAsync(cancellationToken),
                await dbContext.Procedures.CountAsync(cancellationToken),
                await dbContext.Events.CountAsync(cancellationToken),
                await dbContext.Roles.CountAsync(cancellationToken),
                await dbContext.Attachments.CountAsync(cancellationToken),
                await dbContext.Reminders.CountAsync(cancellationToken));
}

