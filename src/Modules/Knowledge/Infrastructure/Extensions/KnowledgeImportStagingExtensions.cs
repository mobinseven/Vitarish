using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Mappers;
using Vitarish.Knowledge.Infrastructure.Data;

namespace Vitarish.Knowledge.Infrastructure.Extensions;

internal static class KnowledgeImportStagingExtensions
{
    public static void StageImport(
        this KnowledgeDbContext dbContext,
        KnowledgeImportResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        dbContext.Sources.AddRange(result.Sources.Select(EntityMapper.ToEntity));
        dbContext.Documents.AddRange(result.Documents.Select(EntityMapper.ToEntity));
        dbContext.Chunks.AddRange(result.Chunks.Select(EntityMapper.ToEntity));
        dbContext.Questions.AddRange(result.Questions.Select(EntityMapper.ToEntity));
    }
}
