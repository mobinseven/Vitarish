using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Vitarish.Authentication.Infrastructure.Options;

public sealed class AuthenticationAttemptKeyOptionsValidator(
    IConfiguration configuration,
    IHostEnvironment? environment) : IValidateOptions<AuthenticationAttemptKeyOptions>
{
    public ValidateOptionsResult Validate(string? name, AuthenticationAttemptKeyOptions options)
    {
        try
        {
            _ = Resolve(options, configuration, environment);
            return ValidateOptionsResult.Success;
        }
        catch (InvalidOperationException exception)
        {
            return ValidateOptionsResult.Fail(exception.Message);
        }
    }

    internal static ResolvedAuthenticationAttemptKeyOptions Resolve(
        AuthenticationAttemptKeyOptions options,
        IConfiguration? configuration,
        IHostEnvironment? environment)
    {
        string realm = string.IsNullOrWhiteSpace(options.Realm)
            ? configuration?["Vitarish:Identity:Authority"] ?? string.Empty
            : options.Realm;
        string currentPepper = string.IsNullOrWhiteSpace(options.CurrentPepper)
            ? configuration?["VITARISH_AUTH_ATTEMPT_KEY_PEPPER"]
                ?? (IsNonServingComposition(environment) ? configuration?["Vitarish:Identity:DataProtectionPassword"] : null)
                ?? string.Empty
            : options.CurrentPepper;

        if (string.IsNullOrWhiteSpace(realm))
            throw new InvalidOperationException("Authentication attempt-key realm must be configured from trusted host state.");
        if (string.IsNullOrWhiteSpace(currentPepper) || currentPepper.Length < 32)
            throw new InvalidOperationException("Authentication attempt-key pepper must be at least 32 characters and supplied by protected configuration.");
        if (!IsValidVersion(options.CurrentVersion))
            throw new InvalidOperationException("Authentication attempt-key version is invalid.");
        if (!string.IsNullOrWhiteSpace(options.PreviousPepper) && options.PreviousPepper.Length < 32)
            throw new InvalidOperationException("Previous authentication attempt-key pepper must be at least 32 characters.");
        if (string.IsNullOrWhiteSpace(options.PreviousPepper) && !string.IsNullOrWhiteSpace(options.PreviousVersion))
            throw new InvalidOperationException("Previous authentication attempt-key version requires a previous pepper during rotation.");
        if (!string.IsNullOrWhiteSpace(options.PreviousPepper) &&
            !IsValidVersion(options.PreviousVersion))
            throw new InvalidOperationException("Previous authentication attempt-key version is invalid during rotation.");

        return new ResolvedAuthenticationAttemptKeyOptions(
            realm.Trim(),
            currentPepper,
            options.CurrentVersion,
            options.PreviousPepper,
            options.PreviousVersion);
    }

    private static bool IsValidVersion(string? version) => !string.IsNullOrWhiteSpace(version) &&
        version.All(character => char.IsLetterOrDigit(character) || character is '.' or '_' or '-');

    private static bool IsNonServingComposition(IHostEnvironment? environment) =>
        environment is null ||
        string.Equals(environment.EnvironmentName, "OpenApiGeneration", StringComparison.OrdinalIgnoreCase) ||
        Environment.CommandLine.Contains("dotnet-getdocument", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(
            Environment.GetEnvironmentVariable(AuthenticationAttemptKeyOptions.OpenApiGenerationEnvironmentVariable),
            "true",
            StringComparison.OrdinalIgnoreCase);
}

internal sealed record ResolvedAuthenticationAttemptKeyOptions(
    string Realm,
    string CurrentPepper,
    string CurrentVersion,
    string? PreviousPepper,
    string? PreviousVersion);
