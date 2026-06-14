using MediatR;
using Vitarish.Knowledge.Application.Abstractions;

namespace Vitarish.Knowledge.Application.Commands;

public sealed record SaveKnowledgeImportCommand(KnowledgeImportResult Result) : IRequest;
