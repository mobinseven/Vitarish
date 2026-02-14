using MediatR;
using Vitarish.Entities.Server.Services;
using Vitarish.Parties.Domain.Descriptors;
using Vitarish.Parties.Domain.Dtos;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Mappers;

namespace Vitarish.Parties.Infrastructure.Services;

/// <summary>
/// Service for managing Tenant entities using the Entities framework.
/// </summary>
public class TenantsService(IMediator mediator) : EntityService<Tenant, TenantDescriptor, TenantReadDto, TenantWriteDto, TenantDescriptorDto>(
        mediator,
        tenant => tenant.ToDto(),
        dto => dto.ToDomain(),
        descriptorDto => descriptorDto.ToDomain());
