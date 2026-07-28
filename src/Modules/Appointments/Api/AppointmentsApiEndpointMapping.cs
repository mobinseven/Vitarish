using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Appointments.Contracts.Dto;
using Vitarish.Appointments.Contracts.Enums;
using Vitarish.Appointments.Contracts.Requests;
using Vitarish.Appointments.Contracts.Results;
using Vitarish.Appointments.Contracts.Routes;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Appointments.Api;

public static class AppointmentsApiEndpointMapping
{
    public static IEndpointRouteBuilder MapAppointmentsApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(AppointmentsApiRoutes.Base);

        // Health endpoint
        _ = group.MapModuleGet("/health", (CancellationToken cancellationToken) => Results.Ok(new AppointmentsCommandResult(true)), "AppointmentsGetHealth", typeof(AppointmentsCommandResult));

        // Bookable dates/slots endpoints
        _ = group.MapModulePost(AppointmentsApiRoutes.BookableDates,
            async (BookableDateQueryRequest request, IShiftBookableDateQueryService query, CancellationToken ct) =>
                Results.Ok(await query.GetBookableDatesAsync(request.ShiftId, new Vitarish.Appointments.Domain.Models.ShiftDateRangeQuery
                {
                    DateFrom = request.From,
                    DateTo = request.To
                }, ct)),
            "AppointmentsListBookableDates", typeof(ShiftReservationReadDto[]));

        _ = group.MapModulePost(AppointmentsApiRoutes.BookableSlots,
            async (BookableSlotQueryRequest request, IShiftBookableDateQueryService query, CancellationToken ct) =>
                Results.Ok(await query.GetBookableSlotsAsync(new Vitarish.Appointments.Domain.Dto.ShiftDescriptorDto { Id = request.ShiftId }, new Vitarish.Appointments.Domain.Models.ShiftDateRangeQuery
                {
                    DateFrom = request.Date,
                    DateTo = request.Date
                }, ct)),
            "AppointmentsListBookableSlots", typeof(BookableShiftSlot[]));

        // Reservations endpoints
        _ = group.MapModulePost(AppointmentsApiRoutes.Reservations,
            async (ListReservationsRequest request, IReservationManager m, CancellationToken ct) =>
                OkList((await m.GetListAsync(request.Descriptor.ToDomain(), ct)).Value?.Select(r => r.ToContract()) ?? []),
            "AppointmentsListReservations", typeof(ReservationReadDto[]));

        _ = group.MapModuleGet(AppointmentsApiRoutes.ReservationById,
            async (Guid id, IReservationManager m, CancellationToken ct) =>
                Maybe((await m.GetAsync(id, ct)).Value?.ToContract()),
            "AppointmentsGetReservation", typeof(ReservationReadDto));

        _ = group.MapModulePost(AppointmentsApiRoutes.Reservations + "/create",
            async (CreateReservationRequest request, IReservationManager m, CancellationToken ct) =>
                Command(await m.ReserveAsync(request.ShiftId, request.Date, request.UserId?.ToString(), request.EstimatedSessionLengthOverride, ct)),
            "AppointmentsCreateReservation", typeof(AppointmentsCommandResult));

        _ = group.MapModulePut(AppointmentsApiRoutes.Reservations,
            async (ReservationWriteDto dto, IReservationManager m, CancellationToken ct) =>
                Command(await m.UpdateAsync(dto.ToDomain(), ct)),
            "AppointmentsUpdateReservation", typeof(AppointmentsCommandResult));

        _ = group.MapModuleDelete(AppointmentsApiRoutes.ReservationById,
            async (Guid id, IReservationManager m, CancellationToken ct) =>
                Command(await m.RemoveAsync(id, ct)),
            "AppointmentsDeleteReservation", typeof(AppointmentsCommandResult));

        _ = group.MapModulePost(AppointmentsApiRoutes.ReservationStatus,
            (Guid id, UpdateReservationStatusRequest request, IReservationManager m, CancellationToken ct) =>
                CommandStatus(request.Status, id, m, ct),
            "AppointmentsReservationStatus", typeof(AppointmentsCommandResult));

        _ = group.MapModulePost(AppointmentsApiRoutes.ReservationMove,
            async (Guid id, MoveReservationRequest request, IReservationManager m, CancellationToken ct) =>
                Command(await m.MoveReservationAsync(id, request.ShiftId, request.Date, ct)),
            "AppointmentsMoveReservation", typeof(AppointmentsCommandResult));

        // Shifts endpoints
        _ = group.MapModulePost(AppointmentsApiRoutes.Shifts,
            async (ListShiftsRequest request, IShiftManager m, CancellationToken ct) =>
                OkList((await m.GetListAsync(request.Descriptor.ToDomain(), ct)).Value?.Select(s => s.ToContract()) ?? []),
            "AppointmentsListShifts", typeof(ShiftReadDto[]));

        _ = group.MapModuleGet(AppointmentsApiRoutes.ShiftById,
            async (Guid id, IShiftManager m, CancellationToken ct) =>
                Maybe((await m.GetAsync(id, ct)).Value?.ToContract()),
            "AppointmentsGetShift", typeof(ShiftReadDto));

        _ = group.MapModulePost(AppointmentsApiRoutes.Shifts + "/create",
            async (ShiftWriteRequest request, IShiftManager m, CancellationToken ct) =>
                Command(await m.AddAsync(request.Shift.ToDomain(), ct)),
            "AppointmentsCreateShift", typeof(AppointmentsCommandResult));

        _ = group.MapModulePut(AppointmentsApiRoutes.Shifts,
            async (ShiftWriteRequest request, IShiftManager m, CancellationToken ct) =>
                Command(await m.UpdateAsync(request.Shift.ToDomain(), ct)),
            "AppointmentsUpdateShift", typeof(AppointmentsCommandResult));

        _ = group.MapModuleDelete(AppointmentsApiRoutes.ShiftById,
            async (Guid id, IShiftManager m, CancellationToken ct) =>
                Command(await m.RemoveAsync(id, ct)),
            "AppointmentsDeleteShift", typeof(AppointmentsCommandResult));

        _ = group.MapModulePost(AppointmentsApiRoutes.ShiftStatus,
            (Guid id, UpdateShiftStatusRequest request, IShiftManager m, CancellationToken ct) =>
                CommandShiftStatus(request.Status, id, m, ct),
            "AppointmentsShiftStatus", typeof(AppointmentsCommandResult));

        // Services endpoints
        _ = group.MapModulePost(AppointmentsApiRoutes.Services,
            async (ListServicesRequest request, IServiceManager m, CancellationToken ct) =>
                OkList((await m.GetListAsync(request.Descriptor.ToDomain(), ct)).Value?.Select(s => s.ToContract()) ?? []),
            "AppointmentsListServices", typeof(ServiceReadDto[]));

        _ = group.MapModuleGet(AppointmentsApiRoutes.ServiceById,
            async (Guid id, IServiceManager m, CancellationToken ct) =>
                Maybe((await m.GetAsync(id, ct)).Value?.ToContract()),
            "AppointmentsGetService", typeof(ServiceReadDto));

        _ = group.MapModulePost(AppointmentsApiRoutes.Services + "/create",
            async (ServiceWriteRequest request, IServiceManager m, CancellationToken ct) =>
                Command(await m.AddAsync(request.Service.ToDomain(), ct)),
            "AppointmentsCreateService", typeof(AppointmentsCommandResult));

        _ = group.MapModulePut(AppointmentsApiRoutes.Services,
            async (ServiceWriteRequest request, IServiceManager m, CancellationToken ct) =>
                Command(await m.UpdateAsync(request.Service.ToDomain(), ct)),
            "AppointmentsUpdateService", typeof(AppointmentsCommandResult));

        _ = group.MapModuleDelete(AppointmentsApiRoutes.ServiceById,
            async (Guid id, IServiceManager m, CancellationToken ct) =>
                Command(await m.RemoveAsync(id, ct)),
            "AppointmentsDeleteService", typeof(AppointmentsCommandResult));

        // Service Types endpoints
        _ = group.MapModulePost(AppointmentsApiRoutes.ServiceTypes,
            async (ListServiceTypesRequest request, IServiceTypeManager m, CancellationToken ct) =>
                OkList((await m.GetListAsync(request.Descriptor.ToDomain(), ct)).Value?.Select(s => s.ToContract()) ?? []),
            "AppointmentsListServiceTypes", typeof(ServiceTypeReadDto[]));

        _ = group.MapModuleGet(AppointmentsApiRoutes.ServiceTypeById,
            async (Guid id, IServiceTypeManager m, CancellationToken ct) =>
                Maybe((await m.GetAsync(id, ct)).Value?.ToContract()),
            "AppointmentsGetServiceType", typeof(ServiceTypeReadDto));

        _ = group.MapModulePost(AppointmentsApiRoutes.ServiceTypes + "/create",
            async (ServiceTypeWriteRequest request, IServiceTypeManager m, CancellationToken ct) =>
                Command(await m.AddAsync(request.ServiceType.ToDomain(), ct)),
            "AppointmentsCreateServiceType", typeof(AppointmentsCommandResult));

        _ = group.MapModulePut(AppointmentsApiRoutes.ServiceTypes,
            async (ServiceTypeWriteRequest request, IServiceTypeManager m, CancellationToken ct) =>
                Command(await m.UpdateAsync(request.ServiceType.ToDomain(), ct)),
            "AppointmentsUpdateServiceType", typeof(AppointmentsCommandResult));

        _ = group.MapModuleDelete(AppointmentsApiRoutes.ServiceTypeById,
            async (Guid id, IServiceTypeManager m, CancellationToken ct) =>
                Command(await m.RemoveAsync(id, ct)),
            "AppointmentsDeleteServiceType", typeof(AppointmentsCommandResult));

        // Service Providers endpoints
        _ = group.MapModulePost(AppointmentsApiRoutes.ServiceProviders,
            async (ListServiceProvidersRequest request, IServiceProviderManager m, CancellationToken ct) =>
                OkList((await m.GetListAsync(request.Descriptor.ToDomain(), ct)).Value?.Select(s => s.ToContract()) ?? []),
            "AppointmentsListServiceProviders", typeof(ServiceProviderReadDto[]));

        _ = group.MapModuleGet(AppointmentsApiRoutes.ServiceProviderById,
            async (Guid id, IServiceProviderManager m, CancellationToken ct) =>
                Maybe((await m.GetAsync(id, ct)).Value?.ToContract()),
            "AppointmentsGetServiceProvider", typeof(ServiceProviderReadDto));

        _ = group.MapModulePost(AppointmentsApiRoutes.ServiceProviders + "/create",
            async (ServiceProviderWriteRequest request, IServiceProviderManager m, CancellationToken ct) =>
                Command(await m.AddAsync(request.ServiceProvider.ToDomain(), ct)),
            "AppointmentsCreateServiceProvider", typeof(AppointmentsCommandResult));

        _ = group.MapModulePut(AppointmentsApiRoutes.ServiceProviders,
            async (ServiceProviderWriteRequest request, IServiceProviderManager m, CancellationToken ct) =>
                Command(await m.UpdateAsync(request.ServiceProvider.ToDomain(), ct)),
            "AppointmentsUpdateServiceProvider", typeof(AppointmentsCommandResult));

        _ = group.MapModuleDelete(AppointmentsApiRoutes.ServiceProviderById,
            async (Guid id, IServiceProviderManager m, CancellationToken ct) =>
                Command(await m.RemoveAsync(id, ct)),
            "AppointmentsDeleteServiceProvider", typeof(AppointmentsCommandResult));

        _ = group.MapModulePost(AppointmentsApiRoutes.LinkServiceProviderUser,
            async (Guid id, LinkServiceProviderUserRequest request, IServiceProviderManager m, CancellationToken ct) =>
                Command(await m.LinkUserAsync(id, request.UserId.ToString(), ct)),
            "AppointmentsLinkServiceProviderUser", typeof(AppointmentsCommandResult));

        _ = group.MapModuleDelete(AppointmentsApiRoutes.UnlinkServiceProviderUser,
            async (Guid id, string userId, IServiceProviderManager m, CancellationToken ct) =>
                Command(await m.UnlinkUserAsync(id, userId, ct)),
            "AppointmentsUnlinkServiceProviderUser", typeof(AppointmentsCommandResult));

        // Reminder Policies endpoints
        _ = group.MapModulePost(AppointmentsApiRoutes.ReminderPolicies,
            async (ShiftReminderPolicyDescriptorDto descriptor, IShiftReminderPolicyManager m, CancellationToken ct) =>
                OkList((await m.GetListAsync(descriptor.ToDomain(), ct)).Value?.Select(p => p.ToContract()) ?? []),
            "AppointmentsListReminderPolicies", typeof(ShiftReminderPolicyReadDto[]));

        _ = group.MapModuleGet(AppointmentsApiRoutes.ReminderPolicyById,
            async (Guid id, IShiftReminderPolicyManager m, CancellationToken ct) =>
                Maybe((await m.GetAsync(id, ct)).Value?.ToContract()),
            "AppointmentsGetReminderPolicy", typeof(ShiftReminderPolicyReadDto));

        _ = group.MapModulePost(AppointmentsApiRoutes.ReminderPolicies + "/create",
            async (ShiftReminderPolicyWriteRequest request, IShiftReminderPolicyManager m, CancellationToken ct) =>
                Command(await m.AddAsync(request.Policy.ToDomain(), ct)),
            "AppointmentsCreateReminderPolicy", typeof(AppointmentsCommandResult));

        _ = group.MapModulePut(AppointmentsApiRoutes.ReminderPolicies,
            async (ShiftReminderPolicyWriteRequest request, IShiftReminderPolicyManager m, CancellationToken ct) =>
                Command(await m.UpdateAsync(request.Policy.ToDomain(), ct)),
            "AppointmentsUpdateReminderPolicy", typeof(AppointmentsCommandResult));

        _ = group.MapModuleDelete(AppointmentsApiRoutes.ReminderPolicyById,
            async (Guid id, IShiftReminderPolicyManager m, CancellationToken ct) =>
                Command(await m.RemoveAsync(id, ct)),
            "AppointmentsDeleteReminderPolicy", typeof(AppointmentsCommandResult));

        // Booking Policies endpoints
        _ = group.MapModulePost(AppointmentsApiRoutes.BookingPolicies,
            async (ShiftBookingPolicyDescriptorDto descriptor, IShiftBookingPolicyManager m, CancellationToken ct) =>
                OkList((await m.GetListAsync(descriptor.ToDomain(), ct)).Value?.Select(p => p.ToContract()) ?? []),
            "AppointmentsListBookingPolicies", typeof(ShiftBookingPolicyReadDto[]));

        _ = group.MapModuleGet(AppointmentsApiRoutes.BookingPolicyById,
            async (Guid id, IShiftBookingPolicyManager m, CancellationToken ct) =>
                Maybe((await m.GetAsync(id, ct)).Value?.ToContract()),
            "AppointmentsGetBookingPolicy", typeof(ShiftBookingPolicyReadDto));

        _ = group.MapModulePost(AppointmentsApiRoutes.BookingPolicies + "/create",
            async (ShiftBookingPolicyWriteRequest request, IShiftBookingPolicyManager m, CancellationToken ct) =>
                Command(await m.AddAsync(request.Policy.ToDomain(), ct)),
            "AppointmentsCreateBookingPolicy", typeof(AppointmentsCommandResult));

        _ = group.MapModulePut(AppointmentsApiRoutes.BookingPolicies,
            async (ShiftBookingPolicyWriteRequest request, IShiftBookingPolicyManager m, CancellationToken ct) =>
                Command(await m.UpdateAsync(request.Policy.ToDomain(), ct)),
            "AppointmentsUpdateBookingPolicy", typeof(AppointmentsCommandResult));

        _ = group.MapModuleDelete(AppointmentsApiRoutes.BookingPolicyById,
            async (Guid id, IShiftBookingPolicyManager m, CancellationToken ct) =>
                Command(await m.RemoveAsync(id, ct)),
            "AppointmentsDeleteBookingPolicy", typeof(AppointmentsCommandResult));

        // Temporary Holds endpoints
        _ = group.MapModulePost(AppointmentsApiRoutes.Holds,
            async (CreateTemporaryHoldRequest request, ITemporaryHoldManager m, CancellationToken ct) =>
            {
                TimeSpan holdDuration = TimeSpan.FromMinutes(15);
                DateTimeOffset expiresAt = DateTimeOffset.UtcNow.Add(holdDuration);
                var result = await m.CreateHoldAsync(request.ShiftId, request.Date, null, holdDuration, ct);
                if (!result.IsSuccess || result.Value == Guid.Empty)
                {
                    var error = result.Errors.FirstOrDefault();
                    return Results.BadRequest(new AppointmentsError(AppointmentsErrorCode.UnexpectedFailure, error?.Message ?? "Hold creation failed."));
                }
                return Results.Ok(new TemporaryHoldAcceptedResponse(result.Value, expiresAt));
            },
            "AppointmentsCreateHold", typeof(TemporaryHoldAcceptedResponse));

        _ = group.MapModulePost(AppointmentsApiRoutes.HoldConfirm,
            async (ConfirmTemporaryHoldRequest request, ITemporaryHoldManager m, CancellationToken ct) =>
                Command(await m.ConfirmHoldAsync(request.HoldId, ct)),
            "AppointmentsConfirmHold", typeof(AppointmentsCommandResult));

        _ = group.MapModulePost(AppointmentsApiRoutes.HoldRelease,
            async (ReleaseTemporaryHoldRequest request, ITemporaryHoldManager m, CancellationToken ct) =>
                Command(await m.ReleaseHoldAsync(request.HoldId, ct)),
            "AppointmentsReleaseHold", typeof(AppointmentsCommandResult));

        return endpoints;
    }

    private static IResult OkList(IEnumerable<object> values) => Results.Ok(values.ToArray());

    private static IResult Maybe<T>(T? value) where T : class => value is null
        ? Results.NotFound(new AppointmentsError(AppointmentsErrorCode.NotFound, "Resource not found."))
        : Results.Ok(value);

    private static IResult Command(EntityResult result)
    {
        if (!result.IsSuccess)
        {
            var error = result.Errors.FirstOrDefault();
            return Results.BadRequest(new AppointmentsError(AppointmentsErrorCode.UnexpectedFailure, error?.Message ?? "Operation failed."));
        }
        return Results.Ok(new AppointmentsCommandResult(true, null));
    }

    private static IResult Command<T>(EntityResult<T> result) where T : class
    {
        if (!result.IsSuccess)
        {
            var error = result.Errors.FirstOrDefault();
            return Results.BadRequest(new AppointmentsError(AppointmentsErrorCode.UnexpectedFailure, error?.Message ?? "Operation failed."));
        }

        Guid? entityId = result.Value?.GetType().GetProperty("Id")?.GetValue(result.Value) as Guid?;
        return Results.Ok(new AppointmentsCommandResult(true, entityId));
    }

    private static async Task<IResult> CommandStatus(ReservationStatus status, Guid reservationId, IReservationManager manager, CancellationToken ct)
    {
        EntityResult result = status switch
        {
            ReservationStatus.Confirmed => await manager.ConfirmReservationAsync(reservationId, ct),
            ReservationStatus.Present => await manager.PresentReservationAsync(reservationId, ct),
            ReservationStatus.Completed => await manager.CompleteReservationAsync(reservationId, ct),
            ReservationStatus.Cancelled => await manager.CancelReservationAsync(reservationId, ct),
            ReservationStatus.Rejected => await manager.RejectReservationAsync(reservationId, ct),
            ReservationStatus.Expired => await manager.ExpireReservationAsync(reservationId, ct),
            _ => EntityResult.Failure(EntityError.Unknown("Unsupported status transition."))
        };
        return Command(result);
    }

    private static async Task<IResult> CommandShiftStatus(ShiftStatus status, Guid shiftId, IShiftManager manager, CancellationToken ct)
    {
        EntityResult result = status switch
        {
            ShiftStatus.Active => await manager.ActivateShiftAsync(shiftId, ct),
            ShiftStatus.Paused => await manager.PauseShiftAsync(shiftId, ct),
            ShiftStatus.Closed => await manager.CloseShiftAsync(shiftId, ct),
            ShiftStatus.Cancelled => await manager.CancelShiftAsync(shiftId, ct),
            ShiftStatus.Archived => await manager.ArchiveShiftAsync(shiftId, ct),
            _ => EntityResult.Failure(EntityError.Unknown("Unsupported status transition."))
        };
        return Command(result);
    }
}
