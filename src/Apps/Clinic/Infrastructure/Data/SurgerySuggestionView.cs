namespace Vitarish.Clinic.Infrastructure.Data;

public static class SurgerySuggestionKinds
{
    public const string Location = "location";
    public const string Role = "role";
}

public sealed class SurgerySuggestionView
{
    public string TenantId { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public long UseCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
