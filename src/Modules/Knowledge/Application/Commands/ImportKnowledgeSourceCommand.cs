using MediatR;
using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Commands;

public sealed record ImportKnowledgeSourceCommand(string? TenantId, ImportKnowledgeSourceInput Input) : IRequest<KnowledgeOwnerImportCommandResult>;
