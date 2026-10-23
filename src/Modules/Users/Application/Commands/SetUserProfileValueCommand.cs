using System.Text.RegularExpressions;
using MediatR;

namespace Vitarish.Users.Application.Commands;

/// <summary>
/// Sets or removes one value in a user's profile without touching the others, so several owners can
/// keep their own entries there. Keys are namespaced by their owner (<c>owner.name</c>); a null value
/// removes the key. With <paramref name="Expected"/>, the write happens only if the entry still holds
/// the expected value, so a read-change-write by its owner cannot overwrite a change made meanwhile.
/// </summary>
public sealed partial record SetUserProfileValueCommand(string UserId, string Key, string? Value, ProfileValueExpectation? Expected = null)
    : IRequest<SetUserProfileValueOutcome>
{
    public const int MaxValueLength = 16_384;

    /// <summary>Owner-namespaced keys only, so this never overwrites the profile's own settings.</summary>
    public static bool IsValidKey(string? key) => key is { Length: <= 128 } && NamespacedKey().IsMatch(key);

    [GeneratedRegex("^[a-z0-9-]+(\\.[a-z0-9-]+)+$", RegexOptions.CultureInvariant)]
    private static partial Regex NamespacedKey();
}

/// <summary>The value an entry must still hold for a write to happen; a null value means the entry must be absent.</summary>
public sealed record ProfileValueExpectation(string? Value);

public enum SetUserProfileValueOutcome
{
    Saved,
    UserNotFound,
    Invalid,
    /// <summary>Another change to the same user landed first; the caller may read again and retry.</summary>
    Conflict
}
