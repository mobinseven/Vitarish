namespace Vitarish.Contents.Infrastructure.Services;

[RegisterTransient]
internal sealed class ContentAssetsService(IMediator mediator)
    : EntityService<ContentAsset, ContentAssetDescriptor, ContentAssetReadDto, ContentAssetWriteDto, ContentAssetDescriptorDto>(
        mediator,
        ContentsEntityMapperExtensions.ToRead,
        ContentsEntityMapperExtensions.ToEntity,
        ContentsEntityMapperExtensions.ToDescriptor),
      IContentAssetsService
{
}
