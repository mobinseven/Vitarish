using Vitarish.Appointments.Contracts.Dto;
using Vitarish.Appointments.Contracts.Models;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Appointments.Client;

public interface IAppointmentsClient :
    IAccessControlledReservationManager,
    IAccessControlledShiftManager,
    IServiceManager,
    IServiceProviderManager,
    IServiceTypeManager,
    IShiftBookingPolicyManager,
    IShiftReminderPolicyManager,
    IShiftBookableDateQueryService,
    ITemporaryHoldManager;

public interface IAccessControlledReservationManager : IReservationManager;
public interface IAccessControlledShiftManager : IShiftManager;

public interface IEntityService<TReadDto, TWriteDto, TDescriptorDto>
{
    Task<EntityResult<TReadDto>> AddAsync(TWriteDto dto, CancellationToken cancellationToken);
    Task<EntityResult<IReadOnlyList<TReadDto>>> AddAsync(IReadOnlyList<TWriteDto> dtos, CancellationToken cancellationToken);
    Task<EntityResult<TReadDto>> UpdateAsync(TWriteDto dto, CancellationToken cancellationToken);
    Task<EntityResult<IReadOnlyList<TReadDto>>> UpdateAsync(IReadOnlyList<TWriteDto> dtos, CancellationToken cancellationToken);
    Task<EntityResult<TReadDto>> AddOrUpdateAsync(TWriteDto dto, CancellationToken cancellationToken);
    Task<EntityResult> RemoveAsync(TWriteDto dto, CancellationToken cancellationToken);
    Task<EntityResult> RemoveAsync(IReadOnlyList<TWriteDto> dtos, CancellationToken cancellationToken);
    Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken);
    Task<EntityResult> RemoveByDescriptorAsync(TDescriptorDto descriptor, CancellationToken cancellationToken);
    Task<EntityResult<TReadDto>> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<EntityResult<int>> CountAsync(TDescriptorDto descriptor, CancellationToken cancellationToken);
    Task<EntityResult<IReadOnlyList<TReadDto>>> GetListAsync(TDescriptorDto descriptor, CancellationToken cancellationToken);
    Task<EntityResult<PartedResult<TReadDto>>> GetPartedAsync(TDescriptorDto descriptor, CancellationToken cancellationToken);
}

public interface IReservationManager : IEntityService<ReservationReadDto, ReservationWriteDto, ReservationDescriptorDto>
{
    Task<EntityResult<ReservationReadDto>> ReserveAsync(Guid shiftId, DateOnly date, string? reservingPartyId, TimeSpan? estimatedSessionLength, CancellationToken cancellationToken);
    Task<EntityResult<ReservationReadDto>> ReserveWithOverrideAsync(Guid shiftId, DateOnly date, string? reservingPartyId, TimeSpan? estimatedSessionLength, CancellationToken cancellationToken);
    Task<EntityResult> ConfirmReservationAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<EntityResult> PresentReservationAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<EntityResult> CompleteReservationAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<EntityResult> CancelReservationAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<EntityResult> RejectReservationAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<EntityResult> ExpireReservationAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<EntityResult> MoveReservationAsync(Guid reservationId, Guid targetShiftId, DateOnly targetDate, CancellationToken cancellationToken);
}
public interface IServiceManager : IEntityService<ServiceReadDto, ServiceWriteDto, ServiceDescriptorDto>;
public interface IServiceProviderManager : IEntityService<ServiceProviderReadDto, ServiceProviderWriteDto, ServiceProviderDescriptorDto>
{
    Task<EntityResult> LinkUserAsync(Guid serviceProviderId, string userId, CancellationToken cancellationToken);
    Task<EntityResult> UnlinkUserAsync(Guid serviceProviderId, string userId, CancellationToken cancellationToken);
}
public interface IServiceTypeManager : IEntityService<ServiceTypeReadDto, ServiceTypeWriteDto, ServiceTypeDescriptorDto>;
public interface IShiftBookingPolicyManager : IEntityService<ShiftBookingPolicyReadDto, ShiftBookingPolicyWriteDto, ShiftBookingPolicyDescriptorDto>;
public interface IShiftReminderPolicyManager : IEntityService<ShiftReminderPolicyReadDto, ShiftReminderPolicyWriteDto, ShiftReminderPolicyDescriptorDto>;
public interface IShiftManager : IEntityService<ShiftReadDto, ShiftWriteDto, ShiftDescriptorDto>
{
    Task<EntityResult> ActivateShiftAsync(Guid shiftId, CancellationToken cancellationToken);
    Task<EntityResult> PauseShiftAsync(Guid shiftId, CancellationToken cancellationToken);
    Task<EntityResult> CloseShiftAsync(Guid shiftId, CancellationToken cancellationToken);
    Task<EntityResult> CancelShiftAsync(Guid shiftId, CancellationToken cancellationToken);
    Task<EntityResult> ArchiveShiftAsync(Guid shiftId, CancellationToken cancellationToken);
}
public interface ITemporaryHoldManager
{
    Task<EntityResult<Guid>> CreateHoldAsync(Guid shiftId, DateOnly date, string? reservingPartyId, TimeSpan duration, CancellationToken cancellationToken);
    Task<EntityResult> ConfirmHoldAsync(Guid holdId, CancellationToken cancellationToken);
    Task<EntityResult> ReleaseHoldAsync(Guid holdId, CancellationToken cancellationToken);
    Task ExpireStaleHoldsAsync(CancellationToken cancellationToken);
}
public interface IShiftBookableDateQueryService
{
    Task<IReadOnlyList<ShiftReservationReadDto>> GetBookableDatesAsync(Guid shiftId, ShiftDateRangeQuery range, CancellationToken cancellationToken);
    Task<IReadOnlyList<BookableShiftSlot>> GetBookableSlotsAsync(ShiftDescriptorDto descriptor, ShiftDateRangeQuery range, CancellationToken cancellationToken);
}
