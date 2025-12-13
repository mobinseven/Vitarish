namespace Vitarish.Sea.Application.Commands.Events;


public record UncommitEventCommand(Guid EventId) : IRequest;

