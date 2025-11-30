using MediatR;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Users.Domain.Entities;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Command to create a new user with phone number.
/// </summary>
public sealed record CreateUserWithPhone(string PhoneNumber, string? FirstName, string? LastName) : IRequest<Result<User>>;
