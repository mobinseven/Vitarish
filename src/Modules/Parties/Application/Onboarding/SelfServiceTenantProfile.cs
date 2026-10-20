namespace Vitarish.Parties.Application.Onboarding;

/// <summary>
/// What every self-service tenant starts as: the app it belongs to, and the business categories
/// that app offers its tenants. The app that offers self-service creation registers exactly one;
/// without it, creation is unavailable. The creator never chooses the app. Like the app's existing
/// tenants, a new one starts with no active-module metadata.
/// </summary>
public sealed record SelfServiceTenantProfile(string AppId, IReadOnlyList<string> Categories)
{
    public bool AllowsCategory(string category) => Categories.Contains(category, StringComparer.Ordinal);
}
