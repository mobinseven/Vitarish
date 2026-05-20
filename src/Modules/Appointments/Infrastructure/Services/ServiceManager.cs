namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class ServiceManager(IMediator mediator)
    : EntityService<Service, AppointmentServiceDescriptor, ServiceReadDto, ServiceWriteDto, ServiceDescriptorDto>(
        mediator,
        service => service.ToDto(),
        dto => dto.ToDomain(),
        dto => dto.ToDomain()),
      IServiceManager;
