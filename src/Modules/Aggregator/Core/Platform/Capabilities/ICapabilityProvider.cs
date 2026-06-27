namespace Vitarish.Modules.Aggregator.Core.Platform.Capabilities;

/// <summary>
/// Declares a set of capabilities for a specific provider/integration.
/// Modules implement this interface to declare what operations each provider supports.
/// </summary>
/// <typeparam name="TCapability">Enum or flag type defining available capabilities.</typeparam>
public interface ICapabilityProvider<TCapability> where TCapability : struct, Enum
{
    /// <summary>Unique identifier for this provider.</summary>
    string ProviderKey { get; }

    /// <summary>Check if a specific capability is supported.</summary>
    bool Supports(TCapability capability);

    /// <summary>Get all supported capabilities.</summary>
    IReadOnlySet<TCapability> GetCapabilities();
}
