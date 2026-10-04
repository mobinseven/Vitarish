using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Vitarish.Users.Application.Commands;

public sealed record UpdateManagedUserCommand(
    string UserId,
    string? Email,
    string? PhoneNumber,
    string? FirstName,
    string? LastName,
    string? Revision) : IRequest<IdentityResult>;
