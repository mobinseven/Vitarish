namespace Vitarish.Journey.Application.Abstractions;

/// <summary>
/// Reads what the request's host is for, as far as the journey cares.
/// </summary>
public interface IJourneyHostReader
{
    /// <summary>True when the request came to the sign-up host, where users create their own tenant.</summary>
    Task<bool> IsSignUpHostAsync(CancellationToken cancellationToken = default);
}
