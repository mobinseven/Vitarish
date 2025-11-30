using MediatR;
using Vorn.Aaas.AuthenticationClients.Domain.Entities;

namespace Vorn.Aaas.AuthenticationClients.Application.Commands;

/// <summary>
/// Command to update an existing <see cref="AuthenticationClient"/>.
/// </summary>
public sealed record UpdateAuthenticationClient(string Id, string Url, string? Title, string? Description, string? LogoUrl, bool AutoAccess) : IRequest;
