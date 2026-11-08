namespace Vitarish.Authentication.Application.Abstractions;

/// <summary>Request-scoped session link supplied only by a validated handoff.</summary>
public sealed class AuthenticationSessionLinkContext
{
    public Guid? CentralSessionId { get; set; }
}
