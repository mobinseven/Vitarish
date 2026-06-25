namespace Vitarish.Search.Contracts.Errors;
public sealed record SearchError(SearchErrorCode Code, string Message, int? StatusCode = null, IReadOnlyDictionary<string,string[]>? ValidationErrors = null);
