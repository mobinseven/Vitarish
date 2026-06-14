using Microsoft.EntityFrameworkCore;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Data;
using Vitarish.Knowledge.Infrastructure.Validators;

namespace Vitarish.Knowledge.Infrastructure.Extensions;

internal static class KnowledgeImportPersistenceGuards
{
    public static async Task EnsureCorpusExistsAsync(
        this KnowledgeDbContext dbContext,
        Guid corpusId,
        string tenantId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        bool corpusExists = await dbContext.Corpora
            .AsNoTracking()
            .AnyAsync(corpus => corpus.Id == corpusId && corpus.TenantId == tenantId, cancellationToken);

        if (!corpusExists)
        {
            throw new InvalidOperationException(
                $"Knowledge corpus '{corpusId}' does not exist for tenant '{tenantId}'.");
        }
    }

    public static KnowledgeSource ValidateReplacementImport(
        Guid sourceId,
        KnowledgeImportResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        KnowledgeImportRules.ValidateImport(result);

        if (result.Sources.Count != 1)
        {
            throw new InvalidOperationException(
                "Source replacement must contain exactly one source.");
        }

        KnowledgeSource source = result.Sources[0];

        if (source.Id != sourceId)
        {
            throw new InvalidOperationException(
                $"Replacement source id '{source.Id}' must match requested source id '{sourceId}'.");
        }

        return source;
    }
}
