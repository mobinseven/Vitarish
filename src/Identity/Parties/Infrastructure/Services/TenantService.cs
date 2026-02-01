using MediatR;
using Vitarish.Entities.Server.Services;
using Vitarish.Parties.Application.Descriptors;
using Vitarish.Parties.Application.Dtos;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Infrastructure.Services;

/// <summary>
/// Service for managing Tenant entities using the Entities framework.
/// </summary>
public class TenantService : EntityService<Tenant, TenantDescriptor, TenantReadDto, TenantWriteDto, TenantDescriptorDto>
{
    public TenantService(IMediator mediator) 
        : base(
            mediator,
            tenant => new TenantReadDto
            {
                Id = tenant.Id,
                OwnerId = tenant.OwnerId,
                Name = tenant.Name,
                LogoUrl = tenant.LogoUrl,
                Description = tenant.Description,
                ClientId = tenant.ClientId,
                Metadata = tenant.Metadata,
                CreatedAt = tenant.CreatedAt,
                CreatedBy = tenant.CreatedBy,
                UpdatedAt = tenant.UpdatedAt,
                UpdatedBy = tenant.UpdatedBy,
                DeletedAt = tenant.DeletedAt,
                DeletedBy = tenant.DeletedBy
            },
            dto => new Tenant
            {
                Id = dto.Id,
                OwnerId = dto.OwnerId,
                Name = dto.Name,
                LogoUrl = dto.LogoUrl,
                Description = dto.Description,
                ClientId = dto.ClientId,
                Metadata = dto.Metadata
            },
            descriptorDto => new TenantDescriptor
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
                OwnerId = descriptorDto.OwnerId,
                Name = descriptorDto.Name,
                ClientId = descriptorDto.ClientId
            })
    {
    }
}
