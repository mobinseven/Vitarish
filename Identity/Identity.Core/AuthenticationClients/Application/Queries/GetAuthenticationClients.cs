using FluentValidation;
using MediatR;
using Vitarish.Identity.Core.AuthenticationClients.Domain.Entities;

namespace Vitarish.Identity.Core.AuthenticationClients.Application.Queries;

/// <summary>
/// Query for all authentication clients.
/// </summary>
public sealed record GetAuthenticationClients : IRequest<IEnumerable<AuthenticationClient>>;

/// <summary>
/// Validator for <see cref="GetAuthenticationClients"/>.
/// </summary>
public sealed class GetAuthenticationClientsValidator : AbstractValidator<GetAuthenticationClients> { }
