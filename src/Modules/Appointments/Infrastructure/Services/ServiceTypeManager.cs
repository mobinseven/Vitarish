using Vitarish.Appointments.Domain.Mappers;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class ServiceTypeManager(IMediator mediator)
    : EntityService<ServiceType, ServiceTypeDescriptor, ServiceTypeReadDto, ServiceTypeWriteDto, ServiceTypeDescriptorDto>(
        mediator,
        serviceType => serviceType.ToRead(),
        dto => dto.ToEntity(),
        dto => dto.ToDescriptor()),
      IServiceTypeManager;
