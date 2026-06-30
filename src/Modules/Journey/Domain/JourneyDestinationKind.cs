namespace Vitarish.Journey.Domain;

/// <summary>
/// The kind of destination the journey resolved to.
/// </summary>
public enum JourneyDestinationKind
{
    /// <summary>A blocking gate requires user action.</summary>
    Gate = 1,

    /// <summary>The requested return URL was safe and used.</summary>
    ReturnUrl = 2,

    /// <summary>The app or workspace dashboard.</summary>
    AppDashboard = 3,

    /// <summary>The user's home page.</summary>
    UserHome = 4,

    /// <summary>The global dashboard fallback.</summary>
    GlobalDashboard = 5
}
