using MediatR;
using Vitarish.Base.Domain.Results;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Command to update a user.
/// </summary>
public sealed record UpdateUser(string Id, string? FirstName, string? LastName) : IRequest<Result>;
