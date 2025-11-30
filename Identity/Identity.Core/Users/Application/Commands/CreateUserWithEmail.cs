using MediatR;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Users.Domain.Entities;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Command to create a new user with email.
/// </summary>
public sealed record CreateUserWithEmail(string Email, string? FirstName, string? LastName) : IRequest<Result<User>>;
