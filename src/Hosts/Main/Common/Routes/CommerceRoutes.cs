namespace Vitarish.Main.Common;

public static partial class AppRoutes
{
    public static class Commerce
    {
        public const string Shop = "/commerce/shop";
        public const string ShopOrders = "/commerce/shop/orders";

        public const string OwnerDashboard = "/commerce/owner";
        public const string OwnerOrders = "/commerce/owner/orders";
        public const string OwnerProducts = "/commerce/owner/products";
        public const string OwnerRecovery = "/commerce/owner/recovery";
        public const string OwnerSettings = "/commerce/owner/settings";

        public const string AdminDashboard = "/commerce/admin";
        public const string AdminProvision = "/commerce/admin/provision";
        public const string AdminSync = "/commerce/admin/sync";
    }
}
