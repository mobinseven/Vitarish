namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class ServiceTypeManager(IMediator mediator)
    : EntityService<ServiceType, ServiceTypeDescriptor, ServiceTypeReadDto, ServiceTypeWriteDto, ServiceTypeDescriptorDto>(
        mediator,
        serviceType => serviceType.ToDto(),
        dto => dto.ToDomain(),
        dto => dto.ToDomain()),
      IServiceTypeManager;
