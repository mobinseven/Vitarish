namespace Vitarish.Communications.Application.Commands;

public record MarkReminderSentCommand(Guid Id, DateTimeOffset SentAt) : IRequest;
