using MediatR;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Queries;

public record GetUserByNormalizedNameQuery(string NormalizedUserName) : IRequest<User?>;
