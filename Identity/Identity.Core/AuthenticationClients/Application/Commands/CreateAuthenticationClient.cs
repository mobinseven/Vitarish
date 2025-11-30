using MediatR;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.AuthenticationClients.Domain.Entities;

namespace Vitarish.Identity.Core.AuthenticationClients.Application.Commands;

/// <summary>
/// Command to create a new <see cref="AuthenticationClient"/>.
/// </summary>
public sealed record CreateAuthenticationClient(string Id, string Url, string? Title, string? Description, string? LogoUrl, bool AutoAccess) : IRequest<Result<AuthenticationClient>>;
