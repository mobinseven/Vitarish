namespace Vitarish.Modules.Base.Core.Endpoints;

public static class ModuleEndpointAuthenticationSchemes
{
    public const string Bearer = ModuleEndpointOpenApiConventions.DefaultBearerScheme;
    public const string Browser = "Identity.Application";
    public const string BrowserOrBearer = Bearer + "," + Browser;
}
