using MediatR;
using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Queries;

public sealed record KnowledgeOwnerCollectionQuery(string? TenantId, string? OwnerId, Guid? SelectedCorpusId, Guid? SelectedSourceId) : IRequest<KnowledgeOwnerCollectionReadModel>;
