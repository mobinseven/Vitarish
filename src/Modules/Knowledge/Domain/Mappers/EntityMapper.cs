using System.Text.Json;

namespace Vitarish.Knowledge.Domain.Mappers;

public partial class EntityMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static KnowledgeCorpus ToModel(KnowledgeCorpusEntity entity) => new() { Id = entity.Id, Name = entity.Name, TenantId = entity.TenantId, Description = entity.Description, IsActive = entity.IsActive };
    public static KnowledgeSource ToModel(KnowledgeSourceEntity entity) => new() { Id = entity.Id, CorpusId = entity.CorpusId, TenantId = entity.TenantId, Name = entity.Name, SourceType = entity.SourceType, OriginalLocation = entity.OriginalLocation, Version = entity.Version, Checksum = entity.Checksum, ImportedAt = entity.ImportedAt };
    public static KnowledgeChunk ToModel(KnowledgeChunkEntity entity) => new() { Id = entity.Id, CorpusId = entity.CorpusId, TenantId = entity.TenantId, SourceId = entity.SourceId, DocumentId = entity.DocumentId, Text = entity.Text, Order = entity.Order, Title = entity.Title, HeadingPath = entity.HeadingPath };
    public static KnowledgeQuestion ToModel(KnowledgeQuestionEntity entity) => new() { Id = entity.Id, CorpusId = entity.CorpusId, Question = entity.Question, ExpectedAnswer = entity.ExpectedAnswer, ExpectedChunkIds = DeserializeStringList(entity.ExpectedChunkIdsJson) };
    public static KnowledgeIndexRun ToModel(KnowledgeIndexRunEntity entity) => new() { Id = entity.Id, CorpusId = entity.CorpusId, TenantId = entity.TenantId, IndexedChunkCount = entity.IndexedChunkCount, Scope = entity.Scope, SourceId = entity.SourceId, CreatedAt = entity.CreatedAt };


    public static KnowledgeCorpusEntity ToEntity(KnowledgeCorpus source) =>
        new()
        {
            Id = source.Id,
            TenantId = source.TenantId,
            Name = source.Name,
            IsActive = source.IsActive,
            Description = source.Description,
        };

    public static KnowledgeSourceEntity ToEntity(KnowledgeSource source) =>
        new()
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
        };

    public static KnowledgeDocumentEntity ToEntity(KnowledgeDocument document) =>
        new()
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
        };

    public static KnowledgeChunkEntity ToEntity(KnowledgeChunk chunk) =>
        new()
        {
            Id = chunk.Id,
            CorpusId = chunk.CorpusId,
            SourceId = chunk.SourceId,
            DocumentId = chunk.DocumentId,
            TenantId = chunk.TenantId,
            Text = chunk.Text,
            Order = chunk.Order,
            Title = chunk.Title,
            HeadingPath = chunk.HeadingPath
        };

    public static KnowledgeQuestionEntity ToEntity(KnowledgeQuestion question) =>
        new()
        {
            Id = question.Id,
            CorpusId = question.CorpusId,
            Question = question.Question,
            ExpectedAnswer = question.ExpectedAnswer,
            ExpectedChunkIdsJson = JsonSerializer.Serialize(
                question.ExpectedChunkIds,
                JsonOptions)
        };
    public static IReadOnlyList<Guid> DeserializeStringList(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<IReadOnlyList<Guid>>(json, JsonOptions) ?? []; }
        catch (JsonException) { return []; }
    }
}
