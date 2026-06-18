using MediatR;
using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Commands;

public sealed record EvaluateKnowledgeCorpusCommand(string? TenantId, EvaluateKnowledgeCorpusInput Input) : IRequest<KnowledgeOwnerEvaluationCommandResult>;
