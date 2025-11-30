using MediatR;
using Vitarish.Base.Domain.Results;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Command to set a user's phone number.
/// </summary>
public sealed record SetUserPhoneNumber(string UserId, string PhoneNumber) : IRequest<Result>;
