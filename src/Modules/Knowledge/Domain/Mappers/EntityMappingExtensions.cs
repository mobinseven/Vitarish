namespace Vitarish.Knowledge.Domain.Mappers;

public static class EntityMappingExtensions
{
    public static KnowledgeCorpus ToModel(this KnowledgeCorpusEntity entity) => EntityMapper.ToModel(entity);
    public static KnowledgeSource ToModel(this KnowledgeSourceEntity entity) => EntityMapper.ToModel(entity);
    public static KnowledgeChunk ToModel(this KnowledgeChunkEntity entity) => EntityMapper.ToModel(entity);
    public static KnowledgeQuestion ToModel(this KnowledgeQuestionEntity entity) => EntityMapper.ToModel(entity);
    public static KnowledgeIndexRun ToModel(this KnowledgeIndexRunEntity entity) => EntityMapper.ToModel(entity);
    public static KnowledgeSourceEntity ToEntity(this KnowledgeSource model) => EntityMapper.ToEntity(model);
    public static KnowledgeCorpusEntity ToEntity(this KnowledgeCorpus model) => EntityMapper.ToEntity(model);
    public static KnowledgeChunkEntity ToEntity(this KnowledgeChunk model) => EntityMapper.ToEntity(model);
    public static KnowledgeQuestionEntity ToEntity(this KnowledgeQuestion model) => EntityMapper.ToEntity(model);
    public static KnowledgeIndexRunEntity ToEntity(this KnowledgeIndexRun model) => EntityMapper.ToEntity(model);
}
