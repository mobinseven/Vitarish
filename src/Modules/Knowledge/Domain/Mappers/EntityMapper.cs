using Riok.Mapperly.Abstractions;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Knowledge.Domain.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public partial class EntityMapper
{
    public static partial KnowledgeCorpus ToModel(KnowledgeCorpusEntity entity);
    public static partial KnowledgeSource ToModel(KnowledgeSourceEntity entity);
    public static partial KnowledgeChunk ToModel(KnowledgeChunkEntity entity);
    public static partial KnowledgeQuestion ToModel(KnowledgeQuestionEntity entity);
    public static partial KnowledgeIndexRun ToModel(KnowledgeIndexRunEntity entity);
    [MapperIgnoreSource(nameof(EntityWriteDto.Metadata))]
    [MapperIgnoreSource(nameof(EntityWriteDto.MetadataValues))]
    public static partial KnowledgeIndexRunEntity ToEntity(KnowledgeIndexRun source);
    [MapperIgnoreSource(nameof(EntityWriteDto.Metadata))]
    [MapperIgnoreSource(nameof(EntityWriteDto.MetadataValues))]
    public static partial KnowledgeCorpusEntity ToEntity(KnowledgeCorpus source);

    [MapperIgnoreSource(nameof(EntityWriteDto.Metadata))]
    [MapperIgnoreSource(nameof(EntityWriteDto.MetadataValues))]
    public static partial KnowledgeSourceEntity ToEntity(KnowledgeSource source);

    [MapperIgnoreSource(nameof(EntityWriteDto.Metadata))]
    [MapperIgnoreSource(nameof(EntityWriteDto.MetadataValues))]
    public static partial KnowledgeDocumentEntity ToEntity(KnowledgeDocument document);

    [MapperIgnoreSource(nameof(EntityWriteDto.Metadata))]
    [MapperIgnoreSource(nameof(EntityWriteDto.MetadataValues))]
    public static partial KnowledgeChunkEntity ToEntity(KnowledgeChunk chunk);

    [MapperIgnoreSource(nameof(EntityWriteDto.Metadata))]
    [MapperIgnoreSource(nameof(EntityWriteDto.MetadataValues))]
    public static partial KnowledgeQuestionEntity ToEntity(KnowledgeQuestion question);
}
