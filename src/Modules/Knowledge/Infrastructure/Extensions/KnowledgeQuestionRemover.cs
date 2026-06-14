using Microsoft.EntityFrameworkCore;
using Vitarish.Knowledge.Domain.Entities;
using Vitarish.Knowledge.Infrastructure.Data;

namespace Vitarish.Knowledge.Infrastructure.Extensions;

internal static class KnowledgeQuestionRemover
{
    public static async Task RemoveQuestionsForChunksAsync(KnowledgeDbContext dbContext, Guid corpusId, IReadOnlyCollection<Guid> chunkIds, CancellationToken cancellationToken)
    {
        if (chunkIds.Count == 0)
        {
            return;
        }

        HashSet<Guid> chunkIdSet = [.. chunkIds];
        KnowledgeQuestionEntity[] rows = [.. (await dbContext.Questions
                .Where(question => question.CorpusId == corpusId)
                .ToListAsync(cancellationToken))
            .Where(question => question.ExpectedChunkIds.Any(chunkIdSet.Contains))];

        dbContext.Questions.RemoveRange(rows);
    }
}
