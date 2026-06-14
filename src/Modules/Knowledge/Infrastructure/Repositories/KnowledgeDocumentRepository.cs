using Microsoft.EntityFrameworkCore;
using Vitarish.Entities.Server.Data;
using Vitarish.Entities.Server.Repositories;
using Vitarish.Knowledge.Domain.Entities;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Data;

namespace Vitarish.Knowledge.Infrastructure.Repositories;

public class KnowledgeDocumentRepository(KnowledgeDbContext dbContext) : EntityRepository<KnowledgeDocumentEntity, KnowledgeDocumentDescriptor, KnowledgeDbContext>(dbContext),
      IEntityRepository<KnowledgeDocumentEntity, KnowledgeDocumentDescriptor>
{
    protected override bool IsDuplicateKeyException(DbUpdateException ex)
        => DatabaseExceptions.IsUniqueConstraintViolation(ex);
}
