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

    public static IReadOnlyList<Guid> DeserializeStringList(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<IReadOnlyList<Guid>>(json, JsonOptions) ?? []; }
        catch (JsonException) { return []; }
    }
}
