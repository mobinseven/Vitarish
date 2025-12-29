using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Vitarish.Users.Application.Queries;

public record GetUserRolesQuery(string UserId) : IRequest<IEnumerable<IdentityRole>>;
