using MediatR;

namespace Vitarish.Users.Application.Queries;

public record CheckUserRoleQuery(string UserId, string RoleName) : IRequest<bool>;
