namespace Vitarish.Search.Domain.Models;

/// <summary>
/// Relative priority tier used to protect exact-match-first entity classes.
/// Higher tiers should receive stronger protection in ranking strategies.
/// </summary>
public enum SearchPriorityTier
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}
