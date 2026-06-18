using MediatR;
using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Commands;

public sealed record SaveKnowledgeImportCommand(KnowledgeImportResult Result) : IRequest;
