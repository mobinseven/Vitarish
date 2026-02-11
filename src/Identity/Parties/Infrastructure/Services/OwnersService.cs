using MediatR;
using Vitarish.Entities.Server.Extensions;
using Vitarish.Entities.Server.Services;
using Vitarish.Parties.Domain.Descriptors;
using Vitarish.Parties.Domain.Dtos;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Infrastructure.Services;

/// <summary>
/// Service for managing Owner entities using the Entities framework.
/// </summary>
public class OwnersService(IMediator mediator) : EntityService<Owner, OwnerDescriptor, OwnerReadDto, OwnerWriteDto, OwnerDescriptorDto>(
        mediator,
        owner => owner.ToDto(),
        dto => dto.ToDomain(),
        descriptorDto => descriptorDto.ToDomain())
{
}
