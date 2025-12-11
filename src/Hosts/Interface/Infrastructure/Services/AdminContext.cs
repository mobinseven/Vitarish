using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Interface.Infrastructure.Services;

internal sealed class AdminContext(IRequestContext vornContext) : IAdminContext
{
    public string? UserId => vornContext.UserId;

    public string? OwnerId => field ??= vornContext.OwnerId;
}
