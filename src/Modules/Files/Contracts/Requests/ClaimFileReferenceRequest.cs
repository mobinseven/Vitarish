namespace Vitarish.Files.Contracts.Requests;

public sealed record ClaimFileReferenceRequest(Guid FileId, Guid TransferId);
