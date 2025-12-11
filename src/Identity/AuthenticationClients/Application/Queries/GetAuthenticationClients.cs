using MediatR;
using Vitarish.AuthenticationClients.Domain.Entities;

namespace Vitarish.AuthenticationClients.Application.Queries;
/// <summary>
/// Query for all authentication clients.
/// </summary>
public sealed record GetAuthenticationClients : IRequest<IEnumerable<AuthenticationClient>>;
