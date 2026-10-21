namespace Vitarish.Parties.Application.SignIn;

/// <summary>
/// The tenant of the shared sign-in host, where people sign in before going to one of their
/// clinics or to the onboarding host. Like the onboarding tenant it holds no practice data; it
/// exists so sign-in there has a tenant to bind sessions to.
/// </summary>
public static class SignInTenant
{
    public const string OwnerName = "Vitarish Platform";
    public const string AppId = "vitarish-sign-in";
    public const string Name = "Vitarish Sign-in";
}

/// <summary>
/// The shared sign-in host's public URL (<c>Vitarish:SignInUrl</c>). When set, startup binds its
/// host to <see cref="SignInTenant"/>.
/// </summary>
public sealed class SignInHostOptions
{
    public string? SignInUrl { get; set; }
}
