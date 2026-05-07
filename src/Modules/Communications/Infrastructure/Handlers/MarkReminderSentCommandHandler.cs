namespace Vitarish.Communications.Infrastructure.Handlers;

internal class MarkReminderSentCommandHandler(IEntityRepository<Reminder, ReminderDescriptor> repository) : IRequestHandler<MarkReminderSentCommand>
{
    public async Task Handle(MarkReminderSentCommand request, CancellationToken cancellationToken)
    {
        Reminder? reminder = await repository.GetAsync(request.Id, cancellationToken);
        if (reminder is null)
        {
            return;
        }
        reminder.MarkSent(request.SentAt);
        _ = await repository.UpdateAsync(reminder, cancellationToken);
    }
}
