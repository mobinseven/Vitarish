namespace Vitarish.Sea.Application.Commands.Roles;


public record AddRoleToEventCommand(Guid EventId, Guid RoleId, bool IsWrite) : IRequest;
