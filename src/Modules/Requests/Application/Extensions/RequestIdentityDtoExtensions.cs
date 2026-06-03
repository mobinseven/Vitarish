using Vitarish.Requests.Application.Commands;
using Vitarish.Requests.Domain.Models;

namespace Vitarish.Requests.Application.Extensions;

public static class RequestIdentityDtoExtensions
{
    public static RecordRequestCommand ToCommand(this RequestIdentityDto dto) => new(
        dto.RequestId,
        dto.Authority,
        dto.ClientId,
        dto.OwnerId,
        dto.TenantId,
        dto.UserId,
        dto.ResourceId,
        dto.TraceId,
        dto.AdditionalInfo);
}
