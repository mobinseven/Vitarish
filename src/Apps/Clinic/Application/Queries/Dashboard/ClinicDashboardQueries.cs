using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Enums;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Domain.Surgery;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Clinic.Application.Queries.Dashboard;

public sealed record DashboardDateRange(DateOnly DateFrom, DateOnly DateTo, int Take)
{
    public static DashboardDateRange Create(DateOnly dateFrom, DateOnly dateTo, int? take)
    {
        int boundedTake = take ?? 4;
        if (dateFrom > dateTo) throw new ArgumentException("dateFrom must not be after dateTo.");
        if (dateTo.DayNumber - dateFrom.DayNumber + 1 > 62) throw new ArgumentException("The date range cannot exceed 62 days.");
        if (boundedTake is < 1 or > 20) throw new ArgumentException("take must be between 1 and 20.");
        return new(dateFrom, dateTo, boundedTake);
    }
}

public sealed record DashboardAppointmentReadModel(Guid ReservationId, string? PatientId, string? PatientDisplayName, Guid? ServiceId, string? ServiceDisplayName, DateOnly Date, TimeOnly? StartTime, string? TimeZone, ReservationStatus Status);
public sealed record DashboardSurgeryReadModel(Guid SurgeryId, Guid PatientId, string? PatientDisplayName, string SurgeryName, string Status, DateTimeOffset ScheduledAt);

public interface IClinicDashboardQueries
{
    Task<IReadOnlyList<DashboardAppointmentReadModel>> ListAppointmentsAsync(DashboardDateRange range, CancellationToken cancellationToken);
    Task<IReadOnlyList<DashboardSurgeryReadModel>> ListSurgeriesAsync(DashboardDateRange range, CancellationToken cancellationToken);
}

public sealed class ClinicDashboardQueries(
    IAccessControlledReservationManager reservations,
    IAccessControlledShiftManager shifts,
    IServiceManager services,
    ISurgeryToolchain surgeries,
    IPatientDirectory patients) : IClinicDashboardQueries
{
    public async Task<IReadOnlyList<DashboardAppointmentReadModel>> ListAppointmentsAsync(DashboardDateRange range, CancellationToken cancellationToken)
    {
        EntityResult<IReadOnlyList<ReservationReadDto>> reservationResult = await reservations.GetListAsync(new()
        {
            DateFrom = range.DateFrom,
            DateTo = range.DateTo,
            Take = range.Take,
            Skip = 0
        }, cancellationToken);
        EnsureSuccess(reservationResult);

        Dictionary<Guid, ShiftReadDto?> shiftById = [];
        Dictionary<Guid, ServiceReadDto?> serviceById = [];
        Dictionary<Guid, string?> patientById = [];
        List<DashboardAppointmentReadModel> result = [];
        foreach (ReservationReadDto reservation in reservationResult.Value ?? [])
        {
            if (!shiftById.TryGetValue(reservation.ShiftId, out ShiftReadDto? shift))
            {
                shift = await ReadOptionalAsync(shifts, reservation.ShiftId, cancellationToken);
                shiftById[reservation.ShiftId] = shift;
            }

            ServiceReadDto? service = null;
            if (shift?.ServiceId is Guid serviceId)
            {
                if (!serviceById.TryGetValue(serviceId, out service))
                {
                    service = await ReadOptionalAsync(services, serviceId, cancellationToken);
                    serviceById[serviceId] = service;
                }
            }

            string? patientId = null;
            string? patientName = null;
            if (Guid.TryParse(reservation.ReservingPartyId, out Guid parsedPatientId))
            {
                patientId = parsedPatientId.ToString();
                if (!patientById.TryGetValue(parsedPatientId, out patientName))
                {
                    patientName = (await patients.FindByIdAsync(parsedPatientId, cancellationToken))?.Name;
                    patientById[parsedPatientId] = patientName;
                }
            }

            result.Add(new(reservation.Id, patientId, patientName, shift?.ServiceId, service?.Name, reservation.Date, shift?.StartTime, shift?.TimeZone, reservation.Status));
        }

        return result
            .OrderBy(item => item.Date)
            .ThenBy(item => item.StartTime ?? TimeOnly.MaxValue)
            .ThenBy(item => item.ReservationId)
            .ToArray();
    }

    public async Task<IReadOnlyList<DashboardSurgeryReadModel>> ListSurgeriesAsync(DashboardDateRange range, CancellationToken cancellationToken)
    {
        IReadOnlyList<SurgeryReadModel> selected = await surgeries.GetRangeAsync(range.DateFrom, range.DateTo, range.Take, cancellationToken);
        Dictionary<Guid, string?> names = [];
        List<DashboardSurgeryReadModel> result = [];
        foreach (SurgeryReadModel surgery in selected)
        {
            if (!names.TryGetValue(surgery.PatientId, out string? patientName))
            {
                patientName = (await patients.FindByIdAsync(surgery.PatientId, cancellationToken))?.Name;
                names[surgery.PatientId] = patientName;
            }
            result.Add(new(surgery.Id, surgery.PatientId, patientName, surgery.Name, surgery.Status.ToString(), surgery.ScheduledAt));
        }
        return result;
    }

    private static async Task<ShiftReadDto?> ReadOptionalAsync(IAccessControlledShiftManager service, Guid id, CancellationToken cancellationToken)
    {
        EntityResult<ShiftReadDto> result = await service.GetAsync(id, cancellationToken);
        return ReadOptional(result);
    }

    private static async Task<ServiceReadDto?> ReadOptionalAsync(IServiceManager service, Guid id, CancellationToken cancellationToken)
    {
        EntityResult<ServiceReadDto> result = await service.GetAsync(id, cancellationToken);
        return ReadOptional(result);
    }

    private static T? ReadOptional<T>(EntityResult<T> result)
    {
        if (result.IsSuccess) return result.Value;
        if (result.Errors.Any(error => error.Code == EntityErrorCode.NotFound)) return default;
        throw new InvalidOperationException("The dashboard dependency could not be queried.");
    }

    private static void EnsureSuccess<T>(EntityResult<T> result)
    {
        if (result.IsFailure) throw new InvalidOperationException("The dashboard dependency could not be queried.");
    }
}
