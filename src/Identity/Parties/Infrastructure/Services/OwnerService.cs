using MediatR;
using Vitarish.Entities.Server.Services;
using Vitarish.Parties.Application.Dtos;
using Vitarish.Parties.Domain.Descriptors;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Infrastructure.Services;

/// <summary>
/// Service for managing Owner entities using the Entities framework.
/// </summary>
public class OwnerService(IMediator mediator) : EntityService<Owner, OwnerDescriptor, OwnerReadDto, OwnerWriteDto, OwnerDescriptorDto>(
        mediator,
        owner => new OwnerReadDto
            {
                Id = owner.Id,
                Name = owner.Name,
                LogoUrl = owner.LogoUrl,
                Description = owner.Description,
                CreatedAt = owner.CreatedAt,
                CreatedBy = owner.CreatedBy,
                UpdatedAt = owner.UpdatedAt,
                UpdatedBy = owner.UpdatedBy,
                DeletedAt = owner.DeletedAt,
                DeletedBy = owner.DeletedBy
            },
        dto => new Owner
            {
                Id = dto.Id,
                Name = dto.Name,
                LogoUrl = dto.LogoUrl,
                Description = dto.Description
            },
        descriptorDto => new OwnerDescriptor
            {
                Id = descriptorDto.Id,
                Skip = descriptorDto.Skip,
                Take = descriptorDto.Take,
                Search = descriptorDto.Search,
                CreatedFrom = descriptorDto.CreatedFrom,
                CreatedTo = descriptorDto.CreatedTo,
                CreatedBy = descriptorDto.CreatedBy,
                UpdatedFrom = descriptorDto.UpdatedFrom,
                UpdatedTo = descriptorDto.UpdatedTo,
                UpdatedBy = descriptorDto.UpdatedBy,
                DeletedFrom = descriptorDto.DeletedFrom,
                DeletedTo = descriptorDto.DeletedTo,
                DeletedBy = descriptorDto.DeletedBy,
                Name = descriptorDto.Name
            })
{
}
