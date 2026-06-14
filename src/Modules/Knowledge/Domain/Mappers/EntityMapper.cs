using Riok.Mapperly.Abstractions;
using System.Text.Json;

namespace Vitarish.Knowledge.Domain.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public partial class EntityMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static partial KnowledgeCorpus ToModel(KnowledgeCorpusEntity entity);
    public static partial KnowledgeSource ToModel(KnowledgeSourceEntity entity);
    public static partial KnowledgeChunk ToModel(KnowledgeChunkEntity entity);
    public static partial KnowledgeQuestion ToModel(KnowledgeQuestionEntity entity);
    public static partial KnowledgeIndexRun ToModel(KnowledgeIndexRunEntity entity);
    public static partial KnowledgeIndexRunEntity ToEntity(KnowledgeIndexRun source);
    public static partial KnowledgeCorpusEntity ToEntity(KnowledgeCorpus source);

    public static partial KnowledgeSourceEntity ToEntity(KnowledgeSource source);

    public static partial KnowledgeDocumentEntity ToEntity(KnowledgeDocument document);

    public static partial KnowledgeChunkEntity ToEntity(KnowledgeChunk chunk);

    public static partial KnowledgeQuestionEntity ToEntity(KnowledgeQuestion question);
    public static IReadOnlyList<Guid> DeserializeStringList(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<IReadOnlyList<Guid>>(json, JsonOptions) ?? []; }
        catch (JsonException) { return []; }
    }
}
