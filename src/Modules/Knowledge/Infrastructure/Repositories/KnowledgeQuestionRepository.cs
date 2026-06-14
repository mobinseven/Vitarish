using Microsoft.EntityFrameworkCore;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Mappers;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Data;

namespace Vitarish.Knowledge.Infrastructure.Repositories;

public sealed class KnowledgeQuestionRepository(KnowledgeDbContext dbContext) : IKnowledgeQuestionRepository
{
    public async Task<IReadOnlyList<KnowledgeQuestion>> GetQuestionsAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        bool corpusExists = await dbContext.Corpora.AsNoTracking().AnyAsync(c => c.Id == corpusId && c.TenantId == tenantId, cancellationToken);
        if (!corpusExists) throw new InvalidOperationException($"Knowledge corpus '{corpusId}' does not exist for tenant '{tenantId}'.");
        return await dbContext.Questions.AsNoTracking().Where(q => q.CorpusId == corpusId).OrderBy(q => q.Id).Select(q => q.ToModel()).ToListAsync(cancellationToken);
    }
}
