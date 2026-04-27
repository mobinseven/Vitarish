namespace Vitarish.Reminders.Application.Commands;

public record MarkReminderSentCommand(Guid Id, DateTimeOffset SentAt) : IRequest;
