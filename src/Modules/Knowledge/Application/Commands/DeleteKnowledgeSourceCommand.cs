using MediatR;
using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Commands;

public sealed record DeleteKnowledgeSourceCommand(string? TenantId, string? RequestingOwnerId, DeleteKnowledgeSourceInput Input) : IRequest<KnowledgeOwnerCommandResult>;
