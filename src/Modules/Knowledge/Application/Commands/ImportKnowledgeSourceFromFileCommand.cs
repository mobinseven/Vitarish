using MediatR;
using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Commands;

public sealed record ImportKnowledgeSourceFromFileCommand(string? TenantId, ImportKnowledgeSourceFromFileInput Input) : IRequest<KnowledgeOwnerImportCommandResult>;
