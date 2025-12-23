using MediatR;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Queries;

public record CheckUserRoleQuery(string RoleName) : IRequest<bool>;
