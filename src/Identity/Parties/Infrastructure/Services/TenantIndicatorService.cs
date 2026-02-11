using MediatR;
using Vitarish.Entities.Server.Extensions;
using Vitarish.Entities.Server.Services;
using Vitarish.Parties.Domain.Descriptors;
using Vitarish.Parties.Domain.Dtos;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Infrastructure.Services;

/// <summary>
/// Service for managing TenantIndicator entities using the Entities framework.
/// </summary>
public class TenantIndicatorService(IMediator mediator) : EntityService<TenantIndicator, TenantIndicatorDescriptor, TenantIndicatorReadDto, TenantIndicatorWriteDto, TenantIndicatorDescriptorDto>(
        mediator,
        indicator => indicator.ToDto(),
        dto => dto.ToDomain(),
        descriptorDto => descriptorDto.ToDomain())
{
}
