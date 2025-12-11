
using FluentValidation;
using MediatR;
using Vitarish.AuthenticationClients.Domain.Entities;

namespace Vitarish.AuthenticationClients.Application.Queries;
/// <summary>
/// Query for all authentication clients.
/// </summary>
public sealed record GetAuthenticationClients : IRequest<IEnumerable<AuthenticationClient>>;

/// <summary>
/// Validator for <see cref="GetAuthenticationClients"/>.
/// </summary>
public sealed class GetAuthenticationClientsValidator : AbstractValidator<GetAuthenticationClients> { }
