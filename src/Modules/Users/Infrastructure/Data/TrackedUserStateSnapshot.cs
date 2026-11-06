using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Data;

/// <summary>
/// Keeps a failed Identity update from leaving its attempted values in the scoped tracker.
/// </summary>
internal sealed class TrackedUserStateSnapshot
{
    private readonly EntityState state;
    private readonly PropertyValues currentValues;
    private readonly PropertyValues originalValues;
    private readonly IReadOnlyDictionary<string, bool> modifiedProperties;

    private TrackedUserStateSnapshot(EntityEntry<User> entry)
    {
        state = entry.State;
        currentValues = entry.CurrentValues.Clone();
        originalValues = entry.OriginalValues.Clone();
        modifiedProperties = entry.Properties.ToDictionary(
            property => property.Metadata.Name,
            property => property.IsModified,
            StringComparer.Ordinal);
    }

    public static TrackedUserStateSnapshot Capture(UsersDbContext dbContext, User user) =>
        new(dbContext.Entry(user));

    public void Restore(UsersDbContext dbContext, User user)
    {
        EntityEntry<User> entry = dbContext.Entry(user);
        entry.CurrentValues.SetValues(currentValues);
        entry.OriginalValues.SetValues(originalValues);
        entry.State = state;

        if (state != EntityState.Modified)
            return;

        foreach ((string propertyName, bool isModified) in modifiedProperties)
            entry.Property(propertyName).IsModified = isModified;
    }
}
