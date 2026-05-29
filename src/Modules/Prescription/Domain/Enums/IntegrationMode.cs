namespace Vitarish.Prescription.Domain.Enums;

/// <summary>
/// Integration mode for connecting to an insurance provider.
/// </summary>
public enum IntegrationMode
{
    DirectApi,
    PortalApi,
    DitasGateway,
    ManualFallback,
    Disabled
}
