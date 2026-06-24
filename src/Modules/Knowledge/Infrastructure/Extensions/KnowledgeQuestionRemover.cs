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
        if (distinctChunkIds.Length == 0)
        {
            return;
        }

        var corpusQuestions = await dbContext.Questions
            .AsNoTracking()
            .Where(question => question.CorpusId == corpusId)
            .Select(question => new { question.Id, question.ExpectedChunkIds })
            .ToArrayAsync(cancellationToken);

        Guid[] idsToRemove = [.. corpusQuestions
            .Where(question => question.ExpectedChunkIds.Any(chunkId => distinctChunkIds.Contains(chunkId)))
            .Select(question => question.Id)];
        if (idsToRemove.Length == 0)
        {
            return;
        }

        KnowledgeQuestionEntity[] rows = await dbContext.Questions
            .Where(question => idsToRemove.Contains(question.Id))
            .ToArrayAsync(cancellationToken);

        dbContext.Questions.RemoveRange(rows);
    }
}
