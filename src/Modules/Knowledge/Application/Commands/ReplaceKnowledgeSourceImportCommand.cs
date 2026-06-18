using MediatR;
using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Commands;

public sealed record ReplaceKnowledgeSourceImportCommand(
    Guid SourceId,
    KnowledgeImportResult Result) : IRequest;
