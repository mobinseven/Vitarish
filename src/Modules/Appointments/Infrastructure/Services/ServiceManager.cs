using Vitarish.Appointments.Domain.Mappers;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class ServiceManager(IMediator mediator)
    : EntityService<Service, AppointmentServiceDescriptor, ServiceReadDto, ServiceWriteDto, ServiceDescriptorDto>(
        mediator,
        service => service.ToRead(),
        dto => dto.ToEntity(),
        dto => dto.ToDescriptor()),
      IServiceManager;
