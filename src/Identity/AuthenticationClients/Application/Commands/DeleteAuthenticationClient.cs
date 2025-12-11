using MediatR;
using Vitarish.AuthenticationClients.Domain.Entities;

namespace Vitarish.AuthenticationClients.Application.Commands;

/// <summary>
/// Command to delete an <see cref="AuthenticationClient"/>.
/// </summary>
public sealed record DeleteAuthenticationClient(string Id) : IRequest;
