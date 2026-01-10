namespace Vitarish.Interfaces.Base.Models;

/// <summary>
/// Represents the current state of the bottom navigation bar.
/// </summary>
/// <param name="Home">The Home button configuration.</param>
/// <param name="Search">The Search button configuration.</param>
/// <param name="Back">The Back button configuration.</param>
/// <param name="Profile">The Profile button configuration.</param>
public sealed record BottomNavState(
    NavButton Home,
    NavButton Search,
    NavButton Back,
    NavButton Profile
);
