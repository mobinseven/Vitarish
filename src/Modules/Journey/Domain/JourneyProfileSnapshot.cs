namespace Vitarish.Journey.Domain;

/// <summary>
/// Snapshot of the user's profile state for journey evaluation.
/// </summary>
public sealed record JourneyProfileSnapshot(
    string UserId,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? PreferredLanguage,
    string? TimeZone,
    bool HasConfirmedEmail,
    bool HasConfirmedPhone
);
