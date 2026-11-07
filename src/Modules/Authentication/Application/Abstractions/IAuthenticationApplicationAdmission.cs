namespace Vitarish.Authentication.Application.Abstractions;

/// <summary>App-owned admission for a registered origin; null delegates to normal tenant membership.</summary>
public interface IAuthenticationApplicationAdmission
{
    bool? Allows(string origin, Guid tenantId, IReadOnlySet<string> roles);
}
