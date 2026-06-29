using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Appointments.Contracts.Dto;
using Vitarish.Appointments.Contracts.Requests;
using Vitarish.Appointments.Contracts.Results;
using Vitarish.Appointments.Contracts.Routes;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Appointments.Api;

public static class AppointmentsApiEndpointMapping
{
    public static IEndpointRouteBuilder MapAppointmentsApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(AppointmentsApiRoutes.Base).WithTags("Appointments");
        _ = group.MapModuleGet("/health", (CancellationToken cancellationToken) => Results.Ok(new AppointmentsCommandResult(true)), "AppointmentsGetHealth", typeof(AppointmentsCommandResult));
        _ = group.MapModulePost(AppointmentsApiRoutes.BookableDates, (BookableDateQueryRequest request, CancellationToken cancellationToken) => Results.Ok(Array.Empty<ShiftReservationReadDto>()), "AppointmentsListBookableDates", typeof(ShiftReservationReadDto[]));
        _ = group.MapModulePost(AppointmentsApiRoutes.BookableSlots, (BookableSlotQueryRequest request, CancellationToken cancellationToken) => Results.Ok(Array.Empty<BookableShiftSlot>()), "AppointmentsListBookableSlots", typeof(BookableShiftSlot[]));
        return endpoints;
    }
}
