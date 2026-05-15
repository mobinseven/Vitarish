using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Contents.Application.Abstractions;

public interface IAssetsService : IEntityService<AssetReadDto, AssetWriteDto, AssetDescriptorDto>;
