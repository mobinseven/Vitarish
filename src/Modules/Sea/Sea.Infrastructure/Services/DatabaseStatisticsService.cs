
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Application.Queries.Statistics;

namespace Vitarish.Sea.Infrastructure.Services;

[RegisterScoped]
internal class DatabaseStatisticsService(IMediator mediator, DatabaseStatisticsMapper mapper) : IDatabaseStatisticsService
{
    public async Task<DatabaseStatisticsDto> GetAsync(CancellationToken cancellationToken)
    {
        return mapper.ToDto(await mediator.Send(new GetDatabaseStatisticsQuery(), cancellationToken));
    }
}

