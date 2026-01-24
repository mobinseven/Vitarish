using Microsoft.AspNetCore.Http;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Application.Abstractions
{
    public interface ITenantResolver
    {
        Task<Tenant?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default);
    }
}