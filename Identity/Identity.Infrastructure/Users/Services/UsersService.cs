using MediatR;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Users.Application.Commands;
using Vitarish.Identity.Core.Users.Application.Queries;
using Vitarish.Identity.Core.Users.Domain.Entities;

namespace Vitarish.Identity.Infrastructure.Users.Services;

/// <inheritdoc />
public sealed class UsersService(ISender sender) : IUsersService
{
    public Task<IEnumerable<User>> GetUsersAsync(CancellationToken ct = default) =>
        sender.Send(new GetUsers(), ct);

    public Task<User?> GetUserAsync(string id, CancellationToken ct = default) =>
        sender.Send(new GetUser(id), ct);

    public Task<User?> GetUserByNameAsync(string userName, CancellationToken ct = default) =>
        sender.Send(new GetUserByName(userName), ct);

    public Task<Result<User>> CreateUserWithPhoneAsync(string phoneNumber, string? firstName, string? lastName, CancellationToken ct = default) =>
        sender.Send(new CreateUserWithPhone(phoneNumber, firstName, lastName), ct);

    public Task<Result<User>> CreateUserWithEmailAsync(string email, string? firstName, string? lastName, CancellationToken ct = default) =>
        sender.Send(new CreateUserWithEmail(email, firstName, lastName), ct);

    public Task<Result> UpdateUserAsync(string id, string? firstName, string? lastName, CancellationToken ct = default) =>
        sender.Send(new UpdateUser(id, firstName, lastName), ct);

    public Task<Result> DeleteUserAsync(string id, CancellationToken ct = default) =>
        sender.Send(new DeleteUser(id), ct);

    public Task<Result> SetUserPhoneNumberAsync(string userId, string phoneNumber, CancellationToken ct = default) =>
        sender.Send(new SetUserPhoneNumber(userId, phoneNumber), ct);

    public Task<Result> SetUserEmailAsync(string userId, string email, CancellationToken ct = default) =>
        sender.Send(new SetUserEmail(userId, email), ct);

    public Task<Dictionary<string, string>> GetUserProfileAsync(string userId, CancellationToken ct = default) =>
        sender.Send(new GetUserProfile(userId), ct);

    public Task<Result> UpdateUserProfileAsync(string userId, Dictionary<string, string> preferences, CancellationToken ct = default) =>
        sender.Send(new UpdateUserProfile(userId, preferences), ct);
}
