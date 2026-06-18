using MediatR;
using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Commands;

public sealed record CreateKnowledgeCorpusCommand(string? TenantId, CreateKnowledgeCorpusInput Input) : IRequest<KnowledgeOwnerCreateCorpusResult>;
