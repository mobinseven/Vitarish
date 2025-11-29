namespace Vorn.Aaas.Users.Presentation.Service;

using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Vorn.Aaas.Users.Application.Commands;
using Vorn.Aaas.Users.Application.Queries;
using Vorn.Aaas.Users.Domain.Entities;

/// <summary>
/// Contract for working with Users inside the application (no HTTP required).
/// </summary>
public interface IUsersService
{
    Task<IEnumerable<User>> GetUsersAsync(CancellationToken ct = default);
    Task<User?> GetUserAsync(string id, CancellationToken ct = default);
    Task<User?> GetUserByNameAsync(string userName, CancellationToken ct = default);
    Task<User?> CreateUserWithPhoneAsync(CreateUserWithPhoneCommand command, CancellationToken ct = default);
    Task<User?> CreateUserWithEmailAsync(CreateUserWithEmailCommand command, CancellationToken ct = default);
    Task<IdentityResult> UpdateUserAsync(UpdateUserCommand command, CancellationToken ct = default);
    Task<IdentityResult> DeleteUserAsync(DeleteUserCommand command, CancellationToken ct = default);
    Task<IList<Claim>> GetUserClaimsAsync(string userId, CancellationToken ct = default);
    Task<IdentityResult> UpdateUserClaimsAsync(UpdateUserClaimsCommand command, CancellationToken ct = default);
    Task<bool> UserHasPasswordAsync(string userId, CancellationToken ct = default);
    Task<bool> CheckUserPasswordAsync(string userId, string password, CancellationToken ct = default);
    Task<IdentityResult> RemoveUserPasswordAsync(string userId, CancellationToken ct = default);
    Task<IdentityResult> AddUserPasswordAsync(string userId, string password, CancellationToken ct = default);
    Task<IdentityResult> SetUserPhoneNumberAsync(string userId, string phoneNumber, CancellationToken ct = default);
    Task<IdentityResult> SetUserEmailAsync(string userId, string email, CancellationToken ct = default);
}

/// <inheritdoc />
public sealed class UsersService(ISender sender) : IUsersService
{
    public Task<IEnumerable<User>> GetUsersAsync(CancellationToken ct = default) =>
        sender.Send(new GetUsersQuery(), ct);

    public Task<User?> GetUserAsync(string id, CancellationToken ct = default) =>
        sender.Send(new GetUserQuery(id), ct);

    public Task<User?> CreateUserWithPhoneAsync(CreateUserWithPhoneCommand command, CancellationToken ct = default) =>
        sender.Send(command, ct);

    public Task<User?> CreateUserWithEmailAsync(CreateUserWithEmailCommand command, CancellationToken ct = default) =>
        sender.Send(command, ct);

    public Task<IdentityResult> UpdateUserAsync(UpdateUserCommand command, CancellationToken ct = default) =>
        sender.Send(command, ct);

    public async Task<IList<Claim>> GetUserClaimsAsync(string userId, CancellationToken ct = default)
    {
        var dtos = await sender.Send(new GetUserClaimsQuery(userId), ct);
        return dtos.Select(d => d.ToClaim()).ToList();
    }

    public Task<IdentityResult> UpdateUserClaimsAsync(UpdateUserClaimsCommand command, CancellationToken ct = default) =>
        sender.Send(command, ct);

    public Task<User?> GetUserByNameAsync(string userName, CancellationToken ct = default) =>
        sender.Send(new GetUserByNameQuery(userName), ct);

    public Task<IdentityResult> DeleteUserAsync(DeleteUserCommand command, CancellationToken ct = default) =>
        sender.Send(command, ct);

    public Task<bool> UserHasPasswordAsync(string userId, CancellationToken ct = default) =>
        sender.Send(new UserHasPasswordQuery(userId), ct);

    public Task<bool> CheckUserPasswordAsync(string userId, string password, CancellationToken ct = default) =>
        sender.Send(new CheckUserPasswordQuery(userId, password), ct);

    public Task<IdentityResult> RemoveUserPasswordAsync(string userId, CancellationToken ct = default) =>
        sender.Send(new RemoveUserPasswordCommand(userId), ct);

    public Task<IdentityResult> AddUserPasswordAsync(string userId, string password, CancellationToken ct = default) =>
        sender.Send(new AddUserPasswordCommand(userId, password), ct);

    public Task<IdentityResult> SetUserPhoneNumberAsync(string userId, string phoneNumber, CancellationToken ct = default) =>
        sender.Send(new SetUserPhoneNumberCommand(userId, phoneNumber), ct);

    public Task<IdentityResult> SetUserEmailAsync(string userId, string email, CancellationToken ct = default) =>
        sender.Send(new SetUserEmailCommand(userId, email), ct);
}

