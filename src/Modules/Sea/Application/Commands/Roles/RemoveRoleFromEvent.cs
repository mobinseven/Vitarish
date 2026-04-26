namespace Vitarish.Sea.Application.Commands.Roles;


public record RemoveRoleFromEventCommand(Guid EventId, Guid RoleId, bool IsWrite) : IRequest;
