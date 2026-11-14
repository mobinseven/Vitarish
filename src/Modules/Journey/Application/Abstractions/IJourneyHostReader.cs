namespace Vitarish.Journey.Application.Abstractions;

/// <summary>
/// Reads what the request's host is for, as far as the journey cares.
/// </summary>
public interface IJourneyHostReader
{
    bool IsCentralDestination(string returnUrl) => false;
    /// <summary>True when the request came to the sign-up host, where users create their own tenant.</summary>
    Task<bool> IsSignUpHostAsync(CancellationToken cancellationToken = default);

    /// <summary>True when the request came to the platform admin host, whose app serves none of the journey's gate pages.</summary>
    Task<bool> IsPlatformAdminHostAsync(CancellationToken cancellationToken = default);

    /// <summary>True when the request came to the shared sign-in host, where users choose which of their clinics to open.</summary>
    Task<bool> IsSignInHostAsync(CancellationToken cancellationToken = default);

    /// <summary>True when the request came to the clinic app's shared front page, which is not a clinic.</summary>
    Task<bool> IsClinicFrontPageAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
}
