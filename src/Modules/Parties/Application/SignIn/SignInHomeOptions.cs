namespace Vitarish.Parties.Application.SignIn;

/// <summary>The platform home URL supplied by the host's application configuration.</summary>
public sealed class SignInHomeOptions
{
    public const string PlatformHomePath = "/home";
    public string LandingUrl { get; set; } = string.Empty;
}
