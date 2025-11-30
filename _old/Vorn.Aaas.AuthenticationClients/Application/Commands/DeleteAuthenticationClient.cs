using MediatR;
using Vorn.Aaas.AuthenticationClients.Domain.Entities;

namespace Vorn.Aaas.AuthenticationClients.Application.Commands;

/// <summary>
/// Command to delete an <see cref="AuthenticationClient"/>.
/// </summary>
public sealed record DeleteAuthenticationClient(string Id) : IRequest;
