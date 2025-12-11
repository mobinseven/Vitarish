using MediatR;
using Vitarish.AuthenticationClients.Domain.Entities;

namespace Vitarish.AuthenticationClients.Application.Commands;

/// <summary>
/// Command to create a new <see cref="AuthenticationClient"/>.
/// </summary>
public sealed record CreateAuthenticationClient(string Id, string Url, string? Title, string? Description, string? LogoUrl, bool AutoAccess) : IRequest<AuthenticationClient?>;
