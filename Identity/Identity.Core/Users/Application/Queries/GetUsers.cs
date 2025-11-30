using FluentValidation;
using MediatR;
using Vitarish.Identity.Core.Users.Domain.Entities;

namespace Vitarish.Identity.Core.Users.Application.Queries;

/// <summary>
/// Query for all users.
/// </summary>
public sealed record GetUsers : IRequest<IEnumerable<User>>;

/// <summary>
/// Validator for <see cref="GetUsers"/>.
/// </summary>
public sealed class GetUsersValidator : AbstractValidator<GetUsers> { }
