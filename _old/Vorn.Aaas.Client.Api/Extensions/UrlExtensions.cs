namespace Vorn.Aaas.Client.Api.Extensions
{
    public static class UrlExtensions
    {
        public static string UrlCombine(string baseUrl, string relativePath) => string.IsNullOrWhiteSpace(baseUrl)
                ? relativePath ?? string.Empty
                : string.IsNullOrWhiteSpace(relativePath)
                ? baseUrl
                : new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), relativePath.TrimStart()).ToString();
    }
}
