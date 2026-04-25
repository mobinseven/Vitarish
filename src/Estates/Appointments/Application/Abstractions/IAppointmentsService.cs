using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Appointments.Application.Abstractions;

public interface IAppointmentsService : IEntityService<AppointmentReadDto, AppointmentWriteDto, AppointmentDescriptorDto>;
