namespace Vitarish.Web.Clients.Extensions;

/// <summary>
/// Extension methods for URL operations.
/// </summary>
public static class UrlExtensions
{
    /// <summary>
    /// Combines a base URL with a relative path.
    /// </summary>
    public static string UrlCombine(string baseUrl, string relativePath) =>
        string.IsNullOrWhiteSpace(baseUrl)
            ? relativePath ?? string.Empty
            : string.IsNullOrWhiteSpace(relativePath)
                ? baseUrl
                : new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), relativePath.TrimStart('/')).ToString();
}
