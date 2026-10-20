namespace Vitarish.Authentication.Contracts.Routes;

public static partial class AuthenticationRoutes
{
    public const string TenantHandoffRedeemPath = "/handoff/redeem";
    public const string TenantHandoffRedeem = BasePath + TenantHandoffRedeemPath;
}

public static class AuthenticationTenantHandoffOperationIds
{
    public const string Redeem = "Authentication_TenantHandoff_Redeem";
}
