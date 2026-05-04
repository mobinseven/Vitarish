namespace Vitarish.Sea.Application.Abstractions;

public interface IDatabaseStatisticsService
{
    Task<DatabaseStatisticsReadDto> GetAsync(CancellationToken cancellationToken);
}

