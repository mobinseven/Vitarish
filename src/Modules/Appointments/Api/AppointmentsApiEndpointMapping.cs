using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Appointments.Contracts.Results;
using Vitarish.Appointments.Contracts.Routes;
using Vitarish.Modules.Base.Endpoints;

namespace Vitarish.Appointments.Api;

public static class AppointmentsApiEndpointMapping
{
    public static IEndpointRouteBuilder MapAppointmentsApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(AppointmentsApiRoutes.Base);
        _ = group.MapGet("/health", (CancellationToken cancellationToken) => Results.Ok(new AppointmentsCommandResult(true)));
        return endpoints;
    }
}
