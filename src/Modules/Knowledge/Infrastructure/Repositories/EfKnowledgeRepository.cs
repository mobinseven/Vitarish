using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Data;

namespace Vitarish.Knowledge.Infrastructure.Repositories;

public sealed class EfKnowledgeRepository(KnowledgeDbContext dbContext) : IKnowledgeRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);


    public async Task SaveCorpusAsync(KnowledgeCorpus corpus, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(corpus);
        ArgumentException.ThrowIfNullOrWhiteSpace(corpus.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(corpus.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(corpus.TenantId);

        KnowledgeCorpusEntity entity = new()
        {
            Id = corpus.Id,
            Name = corpus.Name,
            TenantId = corpus.TenantId,
            Description = corpus.Description,
            IsActive = corpus.IsActive,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Corpora.Update(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<KnowledgeCorpus?> GetCorpusAsync(string corpusId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(corpusId);
        return await dbContext.Corpora.AsNoTracking()
            .Where(corpus => corpus.Id == corpusId)
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

    public async Task<IReadOnlyList<KnowledgeCorpus>> ListCorporaAsync(CancellationToken cancellationToken) =>
        await dbContext.Corpora.AsNoTracking()
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

    public async Task<IReadOnlyList<KnowledgeChunk>> GetChunksAsync(string corpusId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(corpusId);
        return await dbContext.Chunks.AsNoTracking()
            .Where(chunk => chunk.CorpusId == corpusId)
            .OrderBy(chunk => chunk.Order)
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

    public async Task<IReadOnlyList<KnowledgeQuestion>> GetQuestionsAsync(string corpusId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(corpusId);
        List<KnowledgeQuestionEntity> questions = await dbContext.Questions.AsNoTracking()
            .Where(question => question.CorpusId == corpusId)
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

    public async Task SaveAskTraceAsync(KnowledgeAskTrace trace, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(trace);
        dbContext.AskTraces.Add(new KnowledgeAskTraceEntity
        {
            Id = trace.Id,
            CorpusId = trace.CorpusId,
            Question = trace.Question,
            RetrievedChunkIdsJson = JsonSerializer.Serialize(trace.RetrievedChunkIds, JsonOptions),
            GeneratedAnswer = trace.GeneratedAnswer,
            IsAnswered = trace.IsAnswered,
            CreatedAt = trace.CreatedAt
        });
        await dbContext.SaveChangesAsync(cancellationToken);
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
