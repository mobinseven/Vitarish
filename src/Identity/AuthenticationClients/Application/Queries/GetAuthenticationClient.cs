using MediatR;
using Vitarish.AuthenticationClients.Domain.Entities;

namespace Vitarish.AuthenticationClients.Application.Queries;
/// <summary>
/// Query for a single <see cref="AuthenticationClient"/> by identifier.
/// </summary>
public sealed record GetAuthenticationClient(string Id) : IRequest<AuthenticationClient?>;
