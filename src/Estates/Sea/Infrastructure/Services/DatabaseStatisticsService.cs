using Vitarish.Sea.Application.Queries.Statistics;

namespace Vitarish.Sea.Infrastructure.Services;

[RegisterTransient]
internal class DatabaseStatisticsService(IMediator mediator) : IDatabaseStatisticsService
{
    public async Task<DatabaseStatisticsReadDto> GetAsync(CancellationToken cancellationToken) => EntityMappers.ToDto(await mediator.Send(new GetDatabaseStatisticsQuery(), cancellationToken));
}

