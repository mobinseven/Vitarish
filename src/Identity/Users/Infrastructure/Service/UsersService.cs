
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Domain.Models;

namespace Vitarish.Users.Infrastructure.Service;
/// <inheritdoc />
public sealed class UsersService(ISender sender) : IUsersService
{
    public Task<IEnumerable<User>> GetUsersAsync(CancellationToken ct = default) =>
        sender.Send(new GetUsersQuery(), ct);

    public Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken) =>
        sender.Send(new CreateUserCommand(user), cancellationToken);

    public Task<User?> GetUserAsync(string id, CancellationToken ct = default) =>
        sender.Send(new GetUserQuery(id), ct);

    public Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
        GetUserAsync(userId, cancellationToken);

    public Task<User?> CreateUserWithPhoneAsync(CreateUserWithPhoneCommand command, CancellationToken ct = default) =>
        sender.Send(command, ct);

    public Task<User?> CreateUserWithEmailAsync(CreateUserWithEmailCommand command, CancellationToken ct = default) =>
        sender.Send(command, ct);

    public Task<IdentityResult> UpdateUserAsync(UpdateUserCommand command, CancellationToken ct = default) =>
        sender.Send(command, ct);

    public async Task<IList<Claim>> GetUserClaimsAsync(string userId, CancellationToken ct = default)
    {
        IEnumerable<ClaimDto> dtos = await sender.Send(new GetUserClaimsQuery(userId), ct);
        return [.. dtos.Select(d => d.ToClaim())];
    }

    public Task<IEnumerable<IdentityRole>> GetUserRolesAsync(string userId, CancellationToken ct = default) =>
        sender.Send(new GetUserRolesQuery(userId), ct);

    public Task<IdentityResult> UpdateUserClaimsAsync(UpdateUserClaimsCommand command, CancellationToken ct = default) =>
        sender.Send(command, ct);

    public Task<User?> GetUserByNameAsync(string userName, CancellationToken ct = default) =>
        sender.Send(new GetUserByNameQuery(userName), ct);

    public Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
        sender.Send(new GetUserByNormalizedNameQuery(normalizedUserName), cancellationToken);

    public Task<IdentityResult> DeleteUserAsync(DeleteUserCommand command, CancellationToken ct = default) =>
        sender.Send(command, ct);

    public Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken) =>
        sender.Send(new DeleteUserCommand(user.Id), cancellationToken);

    public Task<bool> UserHasPasswordAsync(string userId, CancellationToken ct = default) =>
        sender.Send(new UserHasPasswordQuery(userId), ct);

    public Task<IEnumerable<User>> GetUsersByIdentificationCodeAsync(string identificationCode, CancellationToken ct = default) =>
        sender.Send(new GetUsersByIdentificationCodeQuery(identificationCode), ct);

    public Task<bool> CheckUserRoleAsync(string userId, string roleName, CancellationToken ct = default) =>
        sender.Send(new CheckUserRoleQuery(userId, roleName), ct);
    public Task<bool> CheckUserPasswordAsync(string userId, string password, CancellationToken ct = default) =>
        sender.Send(new CheckUserPasswordQuery(userId, password), ct);

    public Task<bool> CheckUserEmailInUseAsync(string email, string? excludeUserId = null, CancellationToken ct = default) =>
        sender.Send(new CheckUserEmailInUseQuery(email, excludeUserId), ct);

    public Task<bool> CheckUserPhoneNumberInUseAsync(string phoneNumber, string? excludeUserId = null, CancellationToken ct = default) =>
        sender.Send(new CheckUserPhoneNumberInUseQuery(phoneNumber, excludeUserId), ct);

    public Task<IdentityResult> RemoveUserPasswordAsync(string userId, CancellationToken ct = default) =>
        sender.Send(new RemoveUserPasswordCommand(userId), ct);

    public Task<IdentityResult> AddUserPasswordAsync(string userId, string password, CancellationToken ct = default) =>
        sender.Send(new AddUserPasswordCommand(userId, password), ct);

    public Task<IdentityResult> SetUserPhoneNumberAsync(string userId, string phoneNumber, CancellationToken ct = default) =>
        sender.Send(new SetUserPhoneNumberCommand(userId, phoneNumber), ct);

    public Task<IdentityResult> SetUserEmailAsync(string userId, string email, CancellationToken ct = default) =>
        sender.Send(new SetUserEmailCommand(userId, email), ct);

    public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Id);

    public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.UserName);

    public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedUserName);

    public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken) =>
        sender.Send(new UpdateUserCommand(user), cancellationToken);

    public async Task SetSecurityStampAsync(User user, string stamp, CancellationToken cancellationToken)
    {
        IdentityResult result = await sender.Send(new SetUserSecurityStampCommand(user, stamp), cancellationToken);
        EnsureSuccess(result);
    }

    public Task<string?> GetSecurityStampAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.SecurityStamp);

    public async Task AddToRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        IdentityResult result = await sender.Send(new AddUserToRoleCommand(user, roleName), cancellationToken);
        EnsureSuccess(result);
    }

    public async Task RemoveFromRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        IdentityResult result = await sender.Send(new RemoveUserFromRoleCommand(user, roleName), cancellationToken);
        EnsureSuccess(result);
    }

    public async Task<IList<string>> GetRolesAsync(User user, CancellationToken cancellationToken)
    {
        IEnumerable<IdentityRole> roles = await GetUserRolesAsync(user.Id, cancellationToken);
        return [.. roles.Where(r => !string.IsNullOrWhiteSpace(r.Name)).Select(r => r.Name!)];
    }

    public async Task<bool> IsInRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        IList<string> roles = await GetRolesAsync(user, cancellationToken);
        return roles.Any(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IList<User>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        IEnumerable<User> users = await sender.Send(new GetUsersByRoleQuery(roleName), cancellationToken);
        return [.. users];
    }

    public void Dispose()
    {
    }

    private static void EnsureSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            string message = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(message);
        }
    }
}
