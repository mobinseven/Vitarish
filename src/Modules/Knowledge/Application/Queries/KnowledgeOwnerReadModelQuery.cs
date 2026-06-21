using MediatR;
using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Queries;

public sealed record KnowledgeOwnerReadModelQuery(string? TenantId, Guid? SelectedCorpusId) : IRequest<KnowledgeOwnerReadModel>;
