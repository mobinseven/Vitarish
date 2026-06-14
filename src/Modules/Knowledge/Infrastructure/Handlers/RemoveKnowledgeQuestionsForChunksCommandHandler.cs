using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Knowledge.Application.Commands;
using Vitarish.Knowledge.Domain.Entities;
using Vitarish.Knowledge.Infrastructure.Data;

namespace Vitarish.Knowledge.Infrastructure.Handlers;

public sealed class RemoveKnowledgeQuestionsForChunksCommandHandler(KnowledgeDbContext dbContext)
    : IRequestHandler<RemoveKnowledgeQuestionsForChunksCommand, int>
{
    public async Task<int> Handle(
        RemoveKnowledgeQuestionsForChunksCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(request.ChunkIds);

        if (request.CorpusId == Guid.Empty)
        {
            throw new ArgumentException("Corpus id cannot be empty.", nameof(request.CorpusId));
        }

        Guid[] distinctChunkIds = [.. request.ChunkIds.Where(static id => id != Guid.Empty).Distinct()];
        if (distinctChunkIds.Length == 0)
        {
            return 0;
        }

        HashSet<Guid> chunkIdSet = [.. distinctChunkIds];

        KnowledgeQuestionEntity[] rows = [.. (await dbContext.Questions
                .Where(question => question.CorpusId == request.CorpusId)
                .ToListAsync(cancellationToken))
            .Where(question => question.ExpectedChunkIds.Any(chunkIdSet.Contains))];

        if (rows.Length > 0)
        {
            dbContext.Questions.RemoveRange(rows);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        return rows.Length;
    }
}
