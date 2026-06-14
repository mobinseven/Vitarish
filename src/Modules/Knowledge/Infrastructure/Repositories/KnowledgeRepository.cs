using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Entities;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Data;

namespace Vitarish.Knowledge.Infrastructure.Repositories;

public sealed class KnowledgeRepository(KnowledgeDbContext dbContext) : IKnowledgeRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);


    public async Task SaveCorpusAsync(KnowledgeCorpus corpus, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(corpus);
        ArgumentException.ThrowIfNullOrWhiteSpace(corpus.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(corpus.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(corpus.TenantId);

        KnowledgeCorpusEntity? existing = await dbContext.Corpora.FindAsync([corpus.Id], cancellationToken);
        if (existing is not null)
        {
            existing.Name = corpus.Name;
            existing.TenantId = corpus.TenantId;
            existing.Description = corpus.Description;
            existing.IsActive = corpus.IsActive;
        }
        else
        {
            KnowledgeCorpusEntity entity = new()
            {
                Id = corpus.Id,
                Name = corpus.Name,
                TenantId = corpus.TenantId,
                Description = corpus.Description,
                IsActive = corpus.IsActive,
                CreatedAt = DateTimeOffset.UtcNow
            };

            dbContext.Corpora.Add(entity);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<KnowledgeCorpus?> GetCorpusAsync(string corpusId, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(corpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return await dbContext.Corpora.AsNoTracking()
            .Where(corpus => corpus.Id == corpusId && corpus.TenantId == tenantId)
            .Select(corpus => new KnowledgeCorpus
            {
                Id = corpus.Id,
                Name = corpus.Name,
                TenantId = corpus.TenantId,
                Description = corpus.Description,
                IsActive = corpus.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeCorpus>> ListCorporaAsync(string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return await dbContext.Corpora.AsNoTracking()
            .Where(corpus => corpus.TenantId == tenantId)
            .OrderBy(corpus => corpus.Name)
            .Select(corpus => new KnowledgeCorpus
            {
                Id = corpus.Id,
                Name = corpus.Name,
                TenantId = corpus.TenantId,
                Description = corpus.Description,
                IsActive = corpus.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeCorpus>> ListActiveCorporaAsync(CancellationToken cancellationToken) =>
        await dbContext.Corpora.AsNoTracking()
            .Where(corpus => corpus.IsActive)
            .OrderBy(corpus => corpus.TenantId)
            .ThenBy(corpus => corpus.Name)
            .Select(corpus => new KnowledgeCorpus
            {
                Id = corpus.Id,
                Name = corpus.Name,
                TenantId = corpus.TenantId,
                Description = corpus.Description,
                IsActive = corpus.IsActive
            })
            .ToListAsync(cancellationToken);


    public async Task<IReadOnlyList<KnowledgeSource>> GetSourcesAsync(string corpusId, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(corpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return await dbContext.Sources.AsNoTracking()
            .Where(source => source.CorpusId == corpusId && source.TenantId == tenantId)
            .OrderByDescending(source => source.ImportedAt)
            .Select(source => new KnowledgeSource
            {
                Id = source.Id,
                CorpusId = source.CorpusId,
                TenantId = source.TenantId,
                Name = source.Name,
                SourceType = source.SourceType,
                OriginalLocation = source.OriginalLocation,
                Version = source.Version,
                Checksum = source.Checksum,
                ImportedAt = source.ImportedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SourceExistsAsync(string sourceId, string corpusId, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(corpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return await dbContext.Sources.AsNoTracking()
            .AnyAsync(source => source.Id == sourceId && source.CorpusId == corpusId && source.TenantId == tenantId, cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeChunk>> GetChunksAsync(string corpusId, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(corpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return await dbContext.Chunks.AsNoTracking()
            .Where(chunk => chunk.CorpusId == corpusId && chunk.TenantId == tenantId)
            .OrderBy(chunk => chunk.SourceId)
            .ThenBy(chunk => chunk.DocumentId)
            .ThenBy(chunk => chunk.Order)
            .Select(chunk => new KnowledgeChunk
            {
                Id = chunk.Id,
                CorpusId = chunk.CorpusId,
                TenantId = chunk.TenantId,
                SourceId = chunk.SourceId,
                DocumentId = chunk.DocumentId,
                Text = chunk.Text,
                Order = chunk.Order,
                Title = chunk.Title,
                HeadingPath = chunk.HeadingPath
            })
            .ToListAsync(cancellationToken);
    }


    public async Task<IReadOnlyList<KnowledgeChunk>> GetChunksBySourceAsync(string sourceId, string corpusId, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(corpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return await dbContext.Chunks.AsNoTracking()
            .Where(chunk => chunk.SourceId == sourceId && chunk.CorpusId == corpusId && chunk.TenantId == tenantId)
            .OrderBy(chunk => chunk.DocumentId)
            .ThenBy(chunk => chunk.Order)
            .Select(chunk => new KnowledgeChunk
            {
                Id = chunk.Id,
                CorpusId = chunk.CorpusId,
                TenantId = chunk.TenantId,
                SourceId = chunk.SourceId,
                DocumentId = chunk.DocumentId,
                Text = chunk.Text,
                Order = chunk.Order,
                Title = chunk.Title,
                HeadingPath = chunk.HeadingPath
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeChunk>> GetChunksForCorporaAsync(IReadOnlyCollection<string> corpusIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(corpusIds);
        if (corpusIds.Count == 0)
        {
            return [];
        }

        string[] distinctCorpusIds = [.. corpusIds
            .Select(corpusId =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(corpusId);
                return corpusId;
            })
            .Distinct(StringComparer.Ordinal)];

        return await dbContext.Chunks.AsNoTracking()
            .Where(chunk => distinctCorpusIds.Contains(chunk.CorpusId))
            .OrderBy(chunk => chunk.CorpusId)
            .ThenBy(chunk => chunk.SourceId)
            .ThenBy(chunk => chunk.DocumentId)
            .ThenBy(chunk => chunk.Order)
            .Select(chunk => new KnowledgeChunk
            {
                Id = chunk.Id,
                CorpusId = chunk.CorpusId,
                TenantId = chunk.TenantId,
                SourceId = chunk.SourceId,
                DocumentId = chunk.DocumentId,
                Text = chunk.Text,
                Order = chunk.Order,
                Title = chunk.Title,
                HeadingPath = chunk.HeadingPath
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeQuestion>> GetQuestionsAsync(string corpusId, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(corpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        List<KnowledgeQuestionEntity> questions = await dbContext.Questions.AsNoTracking()
            .Join(dbContext.Corpora.AsNoTracking(), question => question.CorpusId, corpus => corpus.Id, (question, corpus) => new { question, corpus.TenantId })
            .Where(row => row.question.CorpusId == corpusId && row.TenantId == tenantId)
            .Select(row => row.question)
            .OrderBy(question => question.Id)
            .ToListAsync(cancellationToken);

        return [.. questions.Select(question => new KnowledgeQuestion
        {
            Id = question.Id,
            CorpusId = question.CorpusId,
            Question = question.Question,
            ExpectedAnswer = question.ExpectedAnswer,
            ExpectedChunkIds = DeserializeStringList(question.ExpectedChunkIdsJson)
        })];
    }

    public async Task SaveImportAsync(KnowledgeImportResult result, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        dbContext.Sources.AddRange(result.Sources.Select(source => new KnowledgeSourceEntity
        {
            Id = source.Id,
            CorpusId = source.CorpusId,
            TenantId = source.TenantId,
            Name = source.Name,
            SourceType = source.SourceType,
            OriginalLocation = source.OriginalLocation,
            Version = source.Version,
            Checksum = source.Checksum,
            ImportedAt = source.ImportedAt
        }));
        dbContext.Documents.AddRange(result.Documents.Select(document => new KnowledgeDocumentEntity
        {
            Id = document.Id,
            CorpusId = document.CorpusId,
            TenantId = document.TenantId,
            SourceId = document.SourceId,
            Title = document.Title,
            Text = document.Text,
            Product = document.Product,
            Version = document.Version,
            Language = document.Language
        }));
        dbContext.Chunks.AddRange(result.Chunks.Select(chunk => new KnowledgeChunkEntity
        {
            Id = chunk.Id,
            CorpusId = chunk.CorpusId,
            TenantId = chunk.TenantId,
            SourceId = chunk.SourceId,
            DocumentId = chunk.DocumentId,
            Text = chunk.Text,
            Order = chunk.Order,
            Title = chunk.Title,
            HeadingPath = chunk.HeadingPath
        }));
        dbContext.Questions.AddRange(result.Questions.Select(question => new KnowledgeQuestionEntity
        {
            Id = question.Id,
            CorpusId = question.CorpusId,
            Question = question.Question,
            ExpectedAnswer = question.ExpectedAnswer,
            ExpectedChunkIdsJson = JsonSerializer.Serialize(question.ExpectedChunkIds, JsonOptions)
        }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }


    public async Task DeleteSourceAsync(string sourceId, string corpusId, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(corpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        KnowledgeSourceEntity? source = await dbContext.Sources
            .FirstOrDefaultAsync(found => found.Id == sourceId && found.CorpusId == corpusId && found.TenantId == tenantId, cancellationToken);
        if (source is null)
        {
            throw new InvalidOperationException($"Knowledge source '{sourceId}' does not exist in corpus '{corpusId}' for tenant '{tenantId}'.");
        }

        List<KnowledgeChunkEntity> chunks = await dbContext.Chunks
            .Where(chunk => chunk.SourceId == sourceId && chunk.CorpusId == corpusId && chunk.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        List<KnowledgeDocumentEntity> documents = await dbContext.Documents
            .Where(document => document.SourceId == sourceId && document.CorpusId == corpusId && document.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        string[] chunkIds = [.. chunks.Select(chunk => chunk.Id)];

        dbContext.Chunks.RemoveRange(chunks);
        dbContext.Documents.RemoveRange(documents);
        dbContext.Sources.Remove(source);
        await RemoveQuestionsForChunksAsync(corpusId, chunkIds, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceSourceAsync(string sourceId, KnowledgeImportResult result, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentNullException.ThrowIfNull(result);
        KnowledgeSource source = result.Sources.Single();
        if (!string.Equals(source.Id, sourceId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Replacement source id '{source.Id}' must match requested source id '{sourceId}'.");
        }

        await DeleteSourceAsync(sourceId, result.CorpusId, source.TenantId, cancellationToken);
        await SaveImportAsync(result, cancellationToken);
    }

    public async Task SaveAskTraceAsync(KnowledgeAskTrace trace, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(trace);
        dbContext.AskTraces.Add(new KnowledgeAskTraceEntity
        {
            Id = trace.Id,
            CorpusId = trace.CorpusId,
            TenantId = trace.TenantId,
            Question = trace.Question,
            RetrievedChunkIdsJson = JsonSerializer.Serialize(trace.RetrievedChunkIds, JsonOptions),
            GeneratedAnswer = trace.GeneratedAnswer,
            MissingInformation = trace.MissingInformation,
            CitationsJson = trace.CitationsJson,
            TraceKind = trace.TraceKind,
            IsAnswered = trace.IsAnswered,
            CreatedAt = trace.CreatedAt
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }


    public async Task SaveIndexRunAsync(KnowledgeIndexRun run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentException.ThrowIfNullOrWhiteSpace(run.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(run.CorpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(run.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(run.Scope);

        dbContext.IndexRuns.Add(new KnowledgeIndexRunEntity
        {
            Id = run.Id,
            CorpusId = run.CorpusId,
            TenantId = run.TenantId,
            IndexedChunkCount = run.IndexedChunkCount,
            Scope = run.Scope,
            SourceId = run.SourceId,
            CreatedAt = run.CreatedAt
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<KnowledgeIndexRun?> GetLatestIndexRunAsync(string corpusId, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(corpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return await dbContext.IndexRuns.AsNoTracking()
            .Where(run => run.CorpusId == corpusId && run.TenantId == tenantId)
            .OrderByDescending(run => run.CreatedAt)
            .Select(run => new KnowledgeIndexRun
            {
                Id = run.Id,
                CorpusId = run.CorpusId,
                TenantId = run.TenantId,
                IndexedChunkCount = run.IndexedChunkCount,
                Scope = run.Scope,
                SourceId = run.SourceId,
                CreatedAt = run.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }


    private async Task RemoveQuestionsForChunksAsync(string corpusId, IReadOnlyCollection<string> chunkIds, CancellationToken cancellationToken)
    {
        if (chunkIds.Count == 0)
        {
            return;
        }

        HashSet<string> chunkIdSet = new(chunkIds, StringComparer.Ordinal);
        List<KnowledgeQuestionEntity> questions = await dbContext.Questions
            .Where(question => question.CorpusId == corpusId)
            .ToListAsync(cancellationToken);

        KnowledgeQuestionEntity[] questionsToRemove = [.. questions.Where(question =>
            DeserializeStringList(question.ExpectedChunkIdsJson).Any(chunkIdSet.Contains))];

        dbContext.Questions.RemoveRange(questionsToRemove);
    }

    private static IReadOnlyList<string> DeserializeStringList(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<string>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
