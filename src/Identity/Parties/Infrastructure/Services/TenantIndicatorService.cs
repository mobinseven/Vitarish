using MediatR;
using Vitarish.Entities.Server.Services;
using Vitarish.Parties.Application.Descriptors;
using Vitarish.Parties.Application.Dtos;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Infrastructure.Services;

/// <summary>
/// Service for managing TenantIndicator entities using the Entities framework.
/// </summary>
public class TenantIndicatorService : EntityService<TenantIndicator, TenantIndicatorDescriptor, TenantIndicatorReadDto, TenantIndicatorWriteDto, TenantIndicatorDescriptorDto>
{
    public TenantIndicatorService(IMediator mediator) 
        : base(
            mediator,
            indicator => new TenantIndicatorReadDto
            {
                Id = indicator.Id,
                TenantId = indicator.TenantId,
                Method = indicator.Method,
                Value = indicator.Value,
                Status = indicator.Status,
                IsPrimary = indicator.IsPrimary,
                Verification = indicator.Verification,
                VerifiedAt = indicator.VerifiedAt,
                LastCheckedAt = indicator.LastCheckedAt,
                CreatedAt = indicator.CreatedAt,
                CreatedBy = indicator.CreatedBy,
                UpdatedAt = indicator.UpdatedAt,
                UpdatedBy = indicator.UpdatedBy,
                DeletedAt = indicator.DeletedAt,
                DeletedBy = indicator.DeletedBy
            },
            dto => new TenantIndicator
            {
                Id = dto.Id,
                TenantId = dto.TenantId,
                Method = dto.Method,
                Value = dto.Value,
                Status = dto.Status,
                IsPrimary = dto.IsPrimary,
                Verification = dto.Verification,
                VerifiedAt = dto.VerifiedAt,
                LastCheckedAt = dto.LastCheckedAt
            },
            descriptorDto => new TenantIndicatorDescriptor
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
                TenantId = descriptorDto.TenantId,
                Method = descriptorDto.Method,
                Value = descriptorDto.Value,
                Status = descriptorDto.Status,
                IsPrimary = descriptorDto.IsPrimary
            })
    {
    }
}
