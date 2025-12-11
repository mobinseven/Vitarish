using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Requests.Application.Commands;
using Vitarish.Requests.Domain.Entities;
using Vitarish.Requests.Domain.Models;
using Vitarish.Requests.Infrastructure.Data;

namespace Vitarish.Requests.Infrastructure.Handlers;

public sealed class RecordRequestIdentityCommandHandler(
    RequestsDbContext dbContext,
    ITenantsService tenantsService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<RecordRequestIdentityCommandHandler> logger)
    : IRequestHandler<RecordRequestIdentityCommand, RequestIdentityDto>
{
    public async Task<RequestIdentityDto> Handle(RecordRequestIdentityCommand request, CancellationToken cancellationToken)
    {
        // Resolve tenant from the current HTTP context's host
        string? resolvedOwnerId = request.OwnerId;
        string? resolvedTenantId = request.TenantId;

        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            try
            {
                Tenant? tenant = await tenantsService.ResolveFromHostAsync(httpContext, cancellationToken);
                if (tenant is not null)
                {
                    resolvedOwnerId = tenant.OwnerId.ToString();
                    resolvedTenantId = tenant.Id.ToString();
                    logger.LogDebug(
                        "Resolved tenant {TenantId} for request {RequestId} from host {Host}",
                        resolvedTenantId,
                        request.RequestId,
                        httpContext.Request.Host.Host);
                }
            }
            catch (Exception ex) when (
                ex is not OutOfMemoryException and
                not StackOverflowException)
            {
                logger.LogWarning(ex, "Failed to resolve tenant for request {RequestId}", request.RequestId);
                // Continue with the values provided in the request
            }
        }

        Request? entity = new()
        {
            RequestId = request.RequestId,
            Authority = request.Authority,
            ClientId = request.ClientId,
            OwnerId = resolvedOwnerId,
            TenantId = resolvedTenantId,
            UserId = request.UserId,
            ResourceId = request.ResourceId,
            TraceId = request.TraceId,
            AdditionalInfo = request.AdditionalInfo,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _ = await dbContext.Requests.AddAsync(entity, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return new RequestIdentityDto(entity.RequestId, entity.Authority, entity.ClientId, entity.OwnerId, entity.TenantId, entity.UserId, entity.ResourceId, entity.TraceId, entity.AdditionalInfo);
    }
}
