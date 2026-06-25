namespace Vitarish.Parties.Contracts.Errors;
public sealed record PartiesError(PartiesErrorCode Code, string Message, IReadOnlyDictionary<string, string[]>? ValidationErrors = null);
