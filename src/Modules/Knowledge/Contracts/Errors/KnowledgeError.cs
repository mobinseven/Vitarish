namespace Vitarish.Knowledge.Contracts.Errors;

public enum KnowledgeErrorCode
{
    None, ValidationFailure, AuthorizationFailure, Forbidden, NotFound, Conflict, FileUnavailable, UnsupportedFileType, ParseFailure, IndexingFailure, RetrievalFailure, ProviderFailure, Timeout, Cancellation, DeserializationFailure, TransportFailure, UnexpectedFailure
}

public sealed record KnowledgeContractError(KnowledgeErrorCode Code, string Message, IReadOnlyDictionary<string, string[]>? ValidationErrors = null, int? StatusCode = null);
