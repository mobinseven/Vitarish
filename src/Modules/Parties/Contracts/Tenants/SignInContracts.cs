namespace Vitarish.Parties.Contracts.Tenants;

/// <summary>A clinic the signed-in user can open from the shared sign-in host.</summary>
/// <param name="Role">The user's role in the clinic, such as <c>TenantOwner</c> or <c>TenantUser</c>.</param>
/// <param name="LogoPath">Where the clinic's logo loads from on <paramref name="Hostname"/>; null when it has none.</param>
public sealed record SignInClinicDto(Guid TenantId, string Name, string Hostname, string Role, string? LogoPath);

public sealed record SignInClinicsResponse(IReadOnlyList<SignInClinicDto> Clinics);

/// <param name="Token">One-time token, valid for a minute on <paramref name="Hostname"/> only, to continue there signed in.</param>
public sealed record SignInHandoffResponse(string Hostname, string Token);
