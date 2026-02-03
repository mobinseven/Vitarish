using MediatR;

namespace Vitarish.Users.Application.Commands;

public record CreateTenantRolesCommand(Guid TenantId) : IRequest;
