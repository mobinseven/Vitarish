using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Commands;
using Vitarish.Knowledge.Domain.Entities;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Data;
using Vitarish.Knowledge.Infrastructure.Extensions;

namespace Vitarish.Knowledge.Infrastructure.Handlers;

public sealed class ReplaceKnowledgeSourceImportCommandHandler(KnowledgeDbContext dbContext)
    : IRequestHandler<ReplaceKnowledgeSourceImportCommand>
{
    public async Task Handle(
        ReplaceKnowledgeSourceImportCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        KnowledgeImportResult result = command.Result;
        KnowledgeSource source = KnowledgeImportPersistenceGuards.ValidateReplacementImport(
            command.SourceId,
            result);

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            KnowledgeSourceEntity existing = await dbContext.Sources
                .FirstOrDefaultAsync(
                    existingSource =>
                        existingSource.Id == command.SourceId &&
                        existingSource.CorpusId == result.CorpusId &&
                        existingSource.TenantId == source.TenantId,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Knowledge source '{command.SourceId}' does not exist in corpus '{result.CorpusId}' for tenant '{source.TenantId}'.");

            Guid[] existingChunkIds = await dbContext.Chunks
                .AsNoTracking()
                .Where(chunk =>
                    chunk.SourceId == command.SourceId &&
                    chunk.CorpusId == result.CorpusId &&
                    chunk.TenantId == source.TenantId)
                .Select(chunk => chunk.Id)
                .ToArrayAsync(cancellationToken);

            _ = dbContext.Sources.Remove(existing);

            await KnowledgeQuestionRemover.RemoveQuestionsForChunksAsync(
                dbContext,
                result.CorpusId,
                existingChunkIds,
                cancellationToken);

            _ = await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.StageImport(result);

            _ = await dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
