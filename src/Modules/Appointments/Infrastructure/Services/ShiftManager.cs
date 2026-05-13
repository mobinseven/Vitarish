namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class ShiftManager(IMediator mediator)
    : EntityService<Shift, ShiftDescriptor, ShiftReadDto, ShiftWriteDto, ShiftDescriptorDto>(
        mediator,
        shift => shift.ToDto(),
        shift => shift.ToDomain(),
        shift => shift.ToDomain()),
      IShiftManager
{
    public Task<EntityResult> ActivateShiftAsync(Guid shiftId, CancellationToken cancellationToken)
        => UpdateStatusAsync(shiftId, ShiftStatus.Active, cancellationToken);

    public Task<EntityResult> PauseShiftAsync(Guid shiftId, CancellationToken cancellationToken)
        => UpdateStatusAsync(shiftId, ShiftStatus.Paused, cancellationToken);

    public Task<EntityResult> CloseShiftAsync(Guid shiftId, CancellationToken cancellationToken)
        => UpdateStatusAsync(shiftId, ShiftStatus.Closed, cancellationToken);

    public Task<EntityResult> CancelShiftAsync(Guid shiftId, CancellationToken cancellationToken)
        => UpdateStatusAsync(shiftId, ShiftStatus.Cancelled, cancellationToken);

    public Task<EntityResult> ArchiveShiftAsync(Guid shiftId, CancellationToken cancellationToken)
        => UpdateStatusAsync(shiftId, ShiftStatus.Archived, cancellationToken);

    private async Task<EntityResult> UpdateStatusAsync(Guid shiftId, ShiftStatus status, CancellationToken cancellationToken)
    {
        await Mediator.Send(new UpdateShiftStatusCommand(shiftId, status), cancellationToken);
        return EntityResult.Success();
    }
}
