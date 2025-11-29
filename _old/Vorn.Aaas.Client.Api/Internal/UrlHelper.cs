namespace Vorn.Aaas.Client.Api.Internal
{
    internal static class UrlHelper
    {
        public static string UrlCombine(string baseUrl, string relativePath)
        {
            if(string.IsNullOrWhiteSpace(baseUrl))
            {
                return relativePath ?? string.Empty;
            }

            if(string.IsNullOrWhiteSpace(relativePath))
            {
                return baseUrl;
            }

            return new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), relativePath.TrimStart()).ToString();
        }
    }
}
