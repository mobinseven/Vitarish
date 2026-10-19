namespace Vitarish.Parties.Application.Onboarding;

/// <summary>
/// What every self-service tenant starts as: the app it belongs to and its active modules.
/// The app that offers self-service creation registers exactly one; without it, creation is
/// unavailable. The creator never chooses these values.
/// </summary>
public sealed record SelfServiceTenantProfile(string AppId, IReadOnlyList<string> ActiveModules);
