namespace Vitarish.Main.Common;

public static partial class AppRoutes
{
    public static class Commerce
    {
        public const string Shop = "/user/commerce/shop";
        public const string ShopOrders = "/user/commerce/shop/orders";

        public const string OwnerDashboard = "/owner/commerce";
        public const string OwnerOrders = "/owner/commerce/orders";
        public const string OwnerProducts = "/owner/commerce/products";
        public const string OwnerRecovery = "/owner/commerce/recovery";
        public const string OwnerSettings = "/owner/commerce/settings";

        public const string AdminDashboard = "/platformadmin/commerce";
        public const string AdminProvision = "/platformadmin/commerce/provision";
        public const string AdminSync = "/platformadmin/commerce/sync";
    }
}
