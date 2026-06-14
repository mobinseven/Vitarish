using MediatR;

namespace Vitarish.Knowledge.Application.Commands;

public sealed record RemoveKnowledgeQuestionsForChunksCommand(
    Guid CorpusId,
    IReadOnlyCollection<Guid> ChunkIds) : IRequest<int>;
