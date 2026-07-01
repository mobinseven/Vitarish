using Vorn.Tamin;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

public enum TaminEndpointMode
{
    Production,
    Sandbox
}

internal static class TaminEndpointModeMapper
{
    public static TaminEndpoint ToSdkEndpoint(this TaminEndpointMode mode) => mode switch
    {
        TaminEndpointMode.Sandbox => TaminEndpoint.Sandbox,
        _ => TaminEndpoint.Production
    };
}
