using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Models;

namespace Vitarish.Contents.Infrastructure.Services;

[RegisterTransient]
internal sealed class AssetsService(
    IMediator mediator,
    IFileInformationService fileInformationService)
    : EntityService<Asset, AssetDescriptor, AssetReadDto, AssetWriteDto, AssetDescriptorDto>(
        mediator,
        ContentsEntityMapperExtensions.ToDto,
        ContentsEntityMapperExtensions.ToDomain,
        ContentsEntityMapperExtensions.ToDescriptor),
      IAssetsService
{
    public new async Task<EntityResult<AssetReadDto>> AddAsync(AssetWriteDto dto, CancellationToken cancellationToken)
    {
        EntityResult validation = await ValidateFileAsync(dto.FileId, cancellationToken);
        return validation.IsFailure
            ? EntityResult<AssetReadDto>.Failure([.. validation.Errors])
            : await base.AddAsync(dto, cancellationToken);
    }

    public new async Task<EntityResult<AssetReadDto>> UpdateAsync(AssetWriteDto dto, CancellationToken cancellationToken)
    {
        EntityResult validation = await ValidateFileAsync(dto.FileId, cancellationToken);
        return validation.IsFailure
            ? EntityResult<AssetReadDto>.Failure([.. validation.Errors])
            : await base.UpdateAsync(dto, cancellationToken);
    }

    private async Task<EntityResult> ValidateFileAsync(Guid fileId, CancellationToken cancellationToken)
    {
        if (fileId == Guid.Empty)
        {
            return EntityResult.Failure(EntityError.Validation("FileId is required.", nameof(fileId)));
        }

        FileInformationReadDto? file = await fileInformationService.GetAsync(fileId, cancellationToken).GetValueOrDefaultAsync();
        return file is null
            ? EntityResult.Failure(EntityError.NotFound("Referenced file record was not found.", nameof(fileId)))
            : EntityResult.Success();
    }
}
