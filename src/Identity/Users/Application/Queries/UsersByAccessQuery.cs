using MediatR;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Queries;

public record UsersByAccessQuery : IRequest<IEnumerable<User>>;
