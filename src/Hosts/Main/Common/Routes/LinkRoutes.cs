namespace Vitarish.Main.Common;

public static partial class AppRoutes
{
    public static class Links
    {
        public const string ShortTemplate = "/r/{code}";
        public const string QrTemplate = "/qr/{code}";
        public const string Disabled = "/link/disabled";
        public const string Expired = "/link/expired";

        /// <summary>Generates <c>/r/{code}</c> – short-link resolver.</summary>
        public static string Short(string code) => $"/r/{CodeValue(code)}";

        /// <summary>Generates <c>/qr/{code}</c> – QR-code resolver.</summary>
        public static string Qr(string code) => $"/qr/{CodeValue(code)}";
    }
}
