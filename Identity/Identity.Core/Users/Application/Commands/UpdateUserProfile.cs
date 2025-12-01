using MediatR;
using Vitarish.Base.Domain.Results;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Command to update a user's profile preferences.
/// </summary>
public sealed record UpdateUserProfile(string UserId, Dictionary<string, string> Preferences) : IRequest<Result>;
