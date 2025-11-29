namespace Vorn.Aaas.Client.Api.Abstractions;

using Vorn.Aaas.Client.Api.Options;

/// <summary>
/// Provides default configuration for a SignalR hub client registration.
/// </summary>
public interface ISignalRClientDefinition
{
    /// <summary>
    /// Configures the <see cref="SignalRClientRegistration"/> used to create hub connections.
    /// </summary>
    /// <param name="registration">The registration to configure.</param>
    static abstract void Configure(SignalRClientRegistration registration);
}
