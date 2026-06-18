using MediatR;
using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Commands;

public sealed record ReplaceKnowledgeSourceCommand(string? TenantId, ReplaceKnowledgeSourceInput Input) : IRequest<KnowledgeOwnerImportCommandResult>;
