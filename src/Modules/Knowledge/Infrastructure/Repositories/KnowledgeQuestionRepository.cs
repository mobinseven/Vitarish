using Microsoft.EntityFrameworkCore;
using Vitarish.Entities.Server.Data;
using Vitarish.Entities.Server.Repositories;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Entities;
using Vitarish.Knowledge.Domain.Mappers;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Data;

namespace Vitarish.Knowledge.Infrastructure.Repositories;

public sealed class KnowledgeQuestionRepository(KnowledgeDbContext dbContext)
    : EntityRepository<KnowledgeQuestionEntity, KnowledgeQuestionDescriptor, KnowledgeDbContext>(dbContext),
      IEntityRepository<KnowledgeQuestionEntity, KnowledgeQuestionDescriptor>,
      IKnowledgeQuestionRepository
{
    protected override bool IsDuplicateKeyException(DbUpdateException ex)
        => DatabaseExceptions.IsUniqueConstraintViolation(ex);

    public async Task<IReadOnlyList<KnowledgeQuestion>> GetQuestionsAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        bool corpusExists = await DbContext.Corpora.AsNoTracking().AnyAsync(c => c.Id == corpusId && c.TenantId == tenantId, cancellationToken);
        return !corpusExists
            ? throw new InvalidOperationException($"Knowledge corpus '{corpusId}' does not exist for tenant '{tenantId}'.")
            : (IReadOnlyList<KnowledgeQuestion>)await DbContext.Questions.AsNoTracking().Where(q => q.CorpusId == corpusId).OrderBy(q => q.Id).Select(q => q.ToModel()).ToListAsync(cancellationToken);
    }
}
