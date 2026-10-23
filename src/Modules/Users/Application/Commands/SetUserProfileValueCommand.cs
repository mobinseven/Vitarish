using System.Text.RegularExpressions;
using MediatR;

namespace Vitarish.Users.Application.Commands;

/// <summary>
/// Sets or removes one value in a user's profile without touching the others, so several owners can
/// keep their own entries there. Keys are namespaced by their owner (<c>owner.name</c>); a null value
/// removes the key.
/// </summary>
public sealed partial record SetUserProfileValueCommand(string UserId, string Key, string? Value) : IRequest<SetUserProfileValueOutcome>
{
    public const int MaxValueLength = 16_384;

    /// <summary>Owner-namespaced keys only, so this never overwrites the profile's own settings.</summary>
    public static bool IsValidKey(string? key) => key is { Length: <= 128 } && NamespacedKey().IsMatch(key);

    [GeneratedRegex("^[a-z0-9-]+(\\.[a-z0-9-]+)+$", RegexOptions.CultureInvariant)]
    private static partial Regex NamespacedKey();
}

public enum SetUserProfileValueOutcome
{
    Saved,
    UserNotFound,
    Invalid,
    /// <summary>Another change to the same user landed first; the caller may read again and retry.</summary>
    Conflict
}
