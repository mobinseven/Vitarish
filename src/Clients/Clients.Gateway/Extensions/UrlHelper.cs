namespace Vitarish.Clients.Gateway.Extensions
{
    internal static class UrlHelper
    {
        public static string UrlCombine(string baseUrl, string relativePath) => string.IsNullOrWhiteSpace(baseUrl)
                ? relativePath ?? string.Empty
                : string.IsNullOrWhiteSpace(relativePath)
                ? baseUrl
                : new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), relativePath.TrimStart()).ToString();
    }
}
