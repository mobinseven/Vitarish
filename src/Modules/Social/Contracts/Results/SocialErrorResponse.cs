using Vitarish.Social.Contracts.Enums;

namespace Vitarish.Social.Contracts.Results;

public sealed record SocialErrorResponse(
    SocialErrorKind Kind,
    string Message,
    int? StatusCode = null,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);

