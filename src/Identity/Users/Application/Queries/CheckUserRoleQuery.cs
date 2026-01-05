using MediatR;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Queries;

public record CheckUserRoleQuery(string UserId, string RoleName) : IRequest<bool>;
