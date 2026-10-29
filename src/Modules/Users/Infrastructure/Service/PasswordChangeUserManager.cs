using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Security;

using UsersIdentityStore = Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<
    Vitarish.Users.Domain.Entities.User,
    Microsoft.AspNetCore.Identity.IdentityRole,
    Vitarish.Users.Infrastructure.Data.UsersDbContext,
    string,
    Microsoft.AspNetCore.Identity.IdentityUserClaim<string>,
    Microsoft.AspNetCore.Identity.IdentityUserRole<string>,
    Microsoft.AspNetCore.Identity.IdentityUserLogin<string>,
    Microsoft.AspNetCore.Identity.IdentityUserToken<string>,
    Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>,
    Microsoft.AspNetCore.Identity.IdentityUserPasskey<string>>;

namespace Vitarish.Users.Infrastructure.Service;

/// <summary>Records password assignment alongside Identity's user update.</summary>
public sealed class PasswordChangeUserManager(
    IUserStore<User> store,
    IOptions<IdentityOptions> options,
    IPasswordHasher<User> passwordHasher,
    IEnumerable<IUserValidator<User>> userValidators,
    IEnumerable<IPasswordValidator<User>> passwordValidators,
    ILookupNormalizer keyNormalizer,
    IdentityErrorDescriber errors,
    IServiceProvider services,
    ILogger<UserManager<User>> logger,
    TimeProvider timeProvider)
    : UserManager<User>(store, options, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
{
    private readonly UsersDbContext usersDbContext = store is UsersIdentityStore efStore
        ? efStore.Context
        : throw new InvalidOperationException("Password assignment requires the configured EF Users store.");
    private readonly AsyncLocal<PasswordAssignmentScope?> passwordAssignmentScope = new();
    private byte[]? passwordAssignmentFingerprint;
    private int passwordAssignmentInProgress;

    public override Task<IdentityResult> CreateAsync(User user, string password) =>
        AssignPasswordAsync(user, () => base.CreateAsync(user, password));

    public override Task<IdentityResult> CreateAsync(User user)
    {
        SetAssignmentFingerprint(user);
        return base.CreateAsync(user);
    }

    public override Task<IdentityResult> AddPasswordAsync(User user, string password) =>
        AssignPasswordAsync(user, () => base.AddPasswordAsync(user, password));

    public override Task<IdentityResult> ChangePasswordAsync(User user, string currentPassword, string newPassword) =>
        AssignPasswordAsync(user, () => base.ChangePasswordAsync(user, currentPassword, newPassword));

    public override Task<IdentityResult> ResetPasswordAsync(User user, string token, string newPassword) =>
        AssignPasswordAsync(user, () => base.ResetPasswordAsync(user, token, newPassword));

    protected override Task<IdentityResult> UpdateUserAsync(User user)
    {
        SetAssignmentFingerprint(user);
        return base.UpdateUserAsync(user);
    }

    private async Task<IdentityResult> AssignPasswordAsync(User user, Func<Task<IdentityResult>> assignment)
    {
        if (Interlocked.CompareExchange(ref passwordAssignmentInProgress, 1, 0) != 0)
            throw new InvalidOperationException("A password assignment is already in progress on this user manager.");

        PasswordAssignmentScope scope = new(user);
        passwordAssignmentScope.Value = scope;
        try
        {
            EntityEntry<User> entry = usersDbContext.Entry(user);
            EntityState previousState = entry.State;
            UserEntrySnapshot snapshot = new(
                previousState,
                entry.CurrentValues.Clone(),
                previousState is EntityState.Detached or EntityState.Added ? null : entry.OriginalValues.Clone(),
                previousState == EntityState.Modified
                    ? entry.Properties.ToDictionary(property => property.Metadata.Name, property => property.IsModified, StringComparer.Ordinal)
                    : new Dictionary<string, bool>(StringComparer.Ordinal));
            user.PasswordChangedAt = timeProvider.GetUtcNow().UtcDateTime;
            IdentityResult result;
            try
            {
                result = await assignment();
            }
            catch
            {
                RestoreAssignment(entry, snapshot, user);
                throw;
            }
            if (!result.Succeeded)
                RestoreAssignment(entry, snapshot, user);
            return result;
        }
        finally
        {
            scope.Deactivate();
            passwordAssignmentScope.Value = null;
            passwordAssignmentFingerprint = null;
            Volatile.Write(ref passwordAssignmentInProgress, 0);
        }
    }

    private void SetAssignmentFingerprint(User user)
    {
        if (passwordAssignmentScope.Value is not { } scope || !scope.AppliesTo(user))
            return;

        byte[]? fingerprint = string.IsNullOrEmpty(user.PasswordHash)
            ? null
            : StoredPasswordHashFingerprint.Create(user.PasswordHash);
        user.PasswordHashFingerprint = fingerprint;
        passwordAssignmentFingerprint = fingerprint;
    }

    private void RestoreAssignment(EntityEntry<User> entry, UserEntrySnapshot snapshot, User user)
    {
        byte[]? failedFingerprint = passwordAssignmentFingerprint;
        RestoreEntry(entry, snapshot);
        if (failedFingerprint is not null && !ReferenceEquals(failedFingerprint, user.PasswordHashFingerprint))
            CryptographicOperations.ZeroMemory(failedFingerprint);
    }

    private static void RestoreEntry(EntityEntry<User> entry, UserEntrySnapshot snapshot)
    {
        entry.CurrentValues.SetValues(snapshot.CurrentValues);
        if (snapshot.OriginalValues is not null)
            entry.OriginalValues.SetValues(snapshot.OriginalValues);
        entry.State = snapshot.State;
        if (snapshot.State == EntityState.Modified)
        {
            foreach (PropertyEntry property in entry.Properties)
                property.IsModified = snapshot.ModifiedProperties[property.Metadata.Name];
        }
    }

    private sealed record UserEntrySnapshot(
        EntityState State,
        PropertyValues CurrentValues,
        PropertyValues? OriginalValues,
        IReadOnlyDictionary<string, bool> ModifiedProperties);

    private sealed class PasswordAssignmentScope(User user)
    {
        private User? scopedUser = user;
        private int active = 1;

        public bool AppliesTo(User candidate) =>
            Volatile.Read(ref active) != 0 && ReferenceEquals(Volatile.Read(ref scopedUser), candidate);

        public void Deactivate()
        {
            Interlocked.Exchange(ref active, 0);
            Interlocked.Exchange(ref scopedUser, null);
        }
    }
}
