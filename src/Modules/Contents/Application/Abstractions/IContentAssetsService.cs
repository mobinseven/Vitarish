using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Contents.Application.Abstractions;

public interface IContentAssetsService : IEntityService<ContentAssetReadDto, ContentAssetWriteDto, ContentAssetDescriptorDto>;
