using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Dto;
using Vitarish.Sea.Application.Queries.Statistics;

namespace Vitarish.Sea.Infrastructure.Handlers;

internal class GetDatabaseStatisticsHandler(SeaDbContext dbContext, IRemindersService remindersService)
    : IRequestHandler<GetDatabaseStatisticsQuery, DatabaseStatistics>
{
    public async Task<DatabaseStatistics> Handle(GetDatabaseStatisticsQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<ReminderReadDto> reminders = await remindersService.GetListAsync(new ReminderDescriptorDto(), cancellationToken).GetValueOrDefaultAsync() ?? [];

        return new DatabaseStatistics(
            await dbContext.Projects.CountAsync(cancellationToken),
            await dbContext.Procedures.CountAsync(cancellationToken),
            await dbContext.Events.CountAsync(cancellationToken),
            await dbContext.Roles.CountAsync(cancellationToken),
            await dbContext.Attachments.CountAsync(cancellationToken),
            reminders.Count);
    }
}

