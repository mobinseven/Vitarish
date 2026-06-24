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

        Guid[] distinctChunkIds = [.. chunkIds.Where(static id => id != Guid.Empty).Distinct()];
        KnowledgeQuestionEntity[] corpusQuestions = await dbContext.Questions
            .Where(question => question.CorpusId == corpusId)
            .ToArrayAsync(cancellationToken);
        KnowledgeQuestionEntity[] rows = [.. corpusQuestions
            .Where(question => question.ExpectedChunkIds.Any(chunkId => distinctChunkIds.Contains(chunkId)))];

        dbContext.Questions.RemoveRange(rows);
    }
}
