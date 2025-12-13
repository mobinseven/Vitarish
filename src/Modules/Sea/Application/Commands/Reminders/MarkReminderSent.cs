namespace Vitarish.Sea.Application.Commands.Reminders;


public record MarkReminderSentCommand(Guid Id, DateTimeOffset SentAt) : IRequest;

