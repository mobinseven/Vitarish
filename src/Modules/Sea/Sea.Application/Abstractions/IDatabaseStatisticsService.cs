using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Application.Abstractions;

public interface IDatabaseStatisticsService
{
    Task<DatabaseStatisticsDto> GetAsync(CancellationToken cancellationToken);
}

