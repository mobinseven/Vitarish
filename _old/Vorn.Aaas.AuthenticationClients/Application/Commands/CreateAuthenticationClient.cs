using MediatR;
using Vorn.Aaas.AuthenticationClients.Domain.Entities;

namespace Vorn.Aaas.AuthenticationClients.Application.Commands;

/// <summary>
/// Command to create a new <see cref="AuthenticationClient"/>.
/// </summary>
public sealed record CreateAuthenticationClient(string Id, string Url, string? Title, string? Description, string? LogoUrl, bool AutoAccess) : IRequest<AuthenticationClient?>;
