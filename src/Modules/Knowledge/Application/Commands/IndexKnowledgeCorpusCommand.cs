using MediatR;
using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Commands;

public sealed record IndexKnowledgeCorpusCommand(string? TenantId, IndexKnowledgeCorpusInput Input) : IRequest<KnowledgeOwnerCommandResult>;
