namespace Vitarish.Knowledge.Domain.Mappers;

public static class EntityMappingExtensions
{
    public static KnowledgeCorpus ToModel(this KnowledgeCorpusEntity entity) => EntityMapper.ToModel(entity);
    public static KnowledgeSource ToModel(this KnowledgeSourceEntity entity) => EntityMapper.ToModel(entity);
    public static KnowledgeChunk ToModel(this KnowledgeChunkEntity entity) => EntityMapper.ToModel(entity);
    public static KnowledgeQuestion ToModel(this KnowledgeQuestionEntity entity) => EntityMapper.ToModel(entity);
    public static KnowledgeIndexRun ToModel(this KnowledgeIndexRunEntity entity) => EntityMapper.ToModel(entity);
}
