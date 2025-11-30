using MediatR;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.AuthenticationClients.Domain.Entities;

namespace Vitarish.Identity.Core.AuthenticationClients.Application.Commands;

/// <summary>
/// Command to delete an <see cref="AuthenticationClient"/>.
/// </summary>
public sealed record DeleteAuthenticationClient(string Id) : IRequest<Result>;
