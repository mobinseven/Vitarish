using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Links.Infrastructure.Services;

[RegisterTransient]
internal sealed class LinksService(IMediator mediator)
    : EntityService<Link, LinkDescriptor, LinkReadDto, LinkWriteDto, LinkDescriptorDto>(
        mediator,
        LinksEntityMapperExtensions.ToRead,
        LinksEntityMapperExtensions.ToEntity,
        LinksEntityMapperExtensions.ToDescriptor),
      ILinksService
{
    public new Task<EntityResult<LinkReadDto>> AddAsync(LinkWriteDto dto, CancellationToken cancellationToken)
    {
        EntityResult validation = ValidateAndNormalize(dto);
        return validation.IsFailure
            ? Task.FromResult(EntityResult<LinkReadDto>.Failure([.. validation.Errors]))
            : base.AddAsync(dto, cancellationToken);
    }

    public new Task<EntityResult<IReadOnlyList<LinkReadDto>>> AddAsync(IReadOnlyList<LinkWriteDto> dtos, CancellationToken cancellationToken)
    {
        EntityResult validation = ValidateAndNormalize(dtos);
        return validation.IsFailure
            ? Task.FromResult(EntityResult<IReadOnlyList<LinkReadDto>>.Failure([.. validation.Errors]))
            : base.AddAsync(dtos, cancellationToken);
    }

    public new Task<EntityResult<LinkReadDto>> UpdateAsync(LinkWriteDto dto, CancellationToken cancellationToken)
    {
        EntityResult validation = ValidateAndNormalize(dto);
        return validation.IsFailure
            ? Task.FromResult(EntityResult<LinkReadDto>.Failure([.. validation.Errors]))
            : base.UpdateAsync(dto, cancellationToken);
    }

    public new Task<EntityResult<IReadOnlyList<LinkReadDto>>> UpdateAsync(IReadOnlyList<LinkWriteDto> dtos, CancellationToken cancellationToken)
    {
        EntityResult validation = ValidateAndNormalize(dtos);
        return validation.IsFailure
            ? Task.FromResult(EntityResult<IReadOnlyList<LinkReadDto>>.Failure([.. validation.Errors]))
            : base.UpdateAsync(dtos, cancellationToken);
    }

    public new Task<EntityResult<LinkReadDto>> AddOrUpdateAsync(LinkWriteDto dto, CancellationToken cancellationToken)
    {
        EntityResult validation = ValidateAndNormalize(dto);
        return validation.IsFailure
            ? Task.FromResult(EntityResult<LinkReadDto>.Failure([.. validation.Errors]))
            : base.AddOrUpdateAsync(dto, cancellationToken);
    }

    private static EntityResult ValidateAndNormalize(IReadOnlyList<LinkWriteDto> dtos)
    {
        List<EntityError> errors = [];

        foreach (LinkWriteDto dto in dtos)
        {
            EntityResult validation = ValidateAndNormalize(dto);
            if (validation.IsFailure)
            {
                errors.AddRange(validation.Errors);
            }
        }

        return errors.Count == 0 ? EntityResult.Success() : EntityResult.Failure([.. errors]);
    }

    private static EntityResult ValidateAndNormalize(LinkWriteDto dto)
    {
        List<EntityError> errors = [];

        if (string.IsNullOrWhiteSpace(dto.Domain))
        {
            errors.Add(EntityError.Validation("Domain is required.", nameof(dto.Domain)));
        }
        else
        {
            dto.Domain = LinkRules.NormalizeDomain(dto.Domain);
        }

        if (string.IsNullOrWhiteSpace(dto.Slug))
        {
            errors.Add(EntityError.Validation("Slug is required.", nameof(dto.Slug)));
        }
        else
        {
            dto.Slug = dto.Slug.Trim();
        }

        if (string.IsNullOrWhiteSpace(dto.DestinationUrl))
        {
            errors.Add(EntityError.Validation("Destination URL is required.", nameof(dto.DestinationUrl)));
        }
        else
        {
            dto.DestinationUrl = LinkRules.NormalizeDestinationUrl(dto.DestinationUrl);
            if (!LinkRules.IsValidDestinationUrl(dto.DestinationUrl))
            {
                errors.Add(EntityError.Validation("Destination URL must be an absolute HTTP or HTTPS URI.", nameof(dto.DestinationUrl)));
            }
        }

        return errors.Count == 0 ? EntityResult.Success() : EntityResult.Failure([.. errors]);
    }
}
