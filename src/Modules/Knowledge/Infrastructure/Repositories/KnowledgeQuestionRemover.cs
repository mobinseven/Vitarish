using Microsoft.EntityFrameworkCore;
using Vitarish.Knowledge.Domain.Entities;
using Vitarish.Knowledge.Domain.Mappers;
using Vitarish.Knowledge.Infrastructure.Data;

namespace Vitarish.Knowledge.Infrastructure.Repositories;

internal static class KnowledgeQuestionRemover
{
    public static async Task RemoveQuestionsForChunksAsync(KnowledgeDbContext dbContext, Guid corpusId, IReadOnlyCollection<Guid> chunkIds, CancellationToken cancellationToken)
    {
        if (chunkIds.Count == 0) return;
        HashSet<Guid> chunkIdSet = [.. chunkIds];
        string[] chunkIdValues = [.. chunkIdSet.Select(id => id.ToString())];
        IQueryable<KnowledgeQuestionEntity> candidates = dbContext.Questions.Where(q => q.CorpusId == corpusId);
        candidates = chunkIdValues.Aggregate(candidates.Where(_ => false), (query, chunkId) => query.Concat(candidates.Where(q => q.ExpectedChunkIdsJson.Contains(chunkId))));
        KnowledgeQuestionEntity[] rows = [.. (await candidates.Distinct().ToListAsync(cancellationToken)).Where(q => EntityMapper.DeserializeStringList(q.ExpectedChunkIdsJson).Any(chunkIdSet.Contains))];
        dbContext.Questions.RemoveRange(rows);
    }
}
