using MediatR;
using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Commands;

public sealed record ReplaceKnowledgeSourceFromFileCommand(string? TenantId, ReplaceKnowledgeSourceFromFileInput Input) : IRequest<KnowledgeOwnerImportCommandResult>;
