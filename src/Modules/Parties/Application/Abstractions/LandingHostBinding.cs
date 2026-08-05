namespace Vitarish.Parties.Application.Abstractions;

public sealed record LandingHostBinding(string Hostname, Guid? TenantId);
public sealed record LandingHostBindingChange(string Hostname, Guid? PreviousTenantId, Guid TenantId, bool Changed);
