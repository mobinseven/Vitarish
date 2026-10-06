using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Vitarish.Users.Application.Commands;

/// <summary>
/// Enables or disables a user account. The requested status is idempotent:
/// replaying the same request succeeds without changing stored state.
/// </summary>
public sealed record ChangeUserAccountStatusCommand(string UserId, bool Enabled) : IRequest<IdentityResult>;
