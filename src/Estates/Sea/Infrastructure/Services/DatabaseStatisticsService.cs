using Vitarish.Sea.Application.Queries.Statistics;

namespace Vitarish.Sea.Infrastructure.Services;

[RegisterTransient]
internal class DatabaseStatisticsService(IMediator mediator, DatabaseStatisticsMapper mapper) : IDatabaseStatisticsService
{
    public async Task<DatabaseStatisticsReadDto> GetAsync(CancellationToken cancellationToken) => mapper.ToDto(await mediator.Send(new GetDatabaseStatisticsQuery(), cancellationToken));
}

