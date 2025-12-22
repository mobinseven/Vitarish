using MediatR;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Queries;

public record GetUsersByRoleQuery(string RoleName) : IRequest<IEnumerable<User>>;
