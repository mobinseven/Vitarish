namespace Vitarish.Appointments.Application.Abstractions;

public interface IShiftManager : IEntityService<ShiftReadDto, ShiftWriteDto, ShiftDescriptorDto>
{
    Task<EntityResult> ActivateShiftAsync(Guid shiftId, CancellationToken cancellationToken);
    Task<EntityResult> PauseShiftAsync(Guid shiftId, CancellationToken cancellationToken);
    Task<EntityResult> CloseShiftAsync(Guid shiftId, CancellationToken cancellationToken);
    Task<EntityResult> CancelShiftAsync(Guid shiftId, CancellationToken cancellationToken);
    Task<EntityResult> ArchiveShiftAsync(Guid shiftId, CancellationToken cancellationToken);
}
