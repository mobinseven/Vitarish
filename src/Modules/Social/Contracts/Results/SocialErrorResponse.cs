namespace Vitarish.Social.Contracts;

public sealed record SocialErrorResponse(
    SocialErrorKind Kind,
    string Message,
    int? StatusCode = null,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);

