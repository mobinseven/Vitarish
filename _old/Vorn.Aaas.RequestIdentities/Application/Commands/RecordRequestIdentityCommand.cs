
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Vorn.Aaas.Parties.Application.Services;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.RequestIdentities.Domain.Entities;
using Vorn.Aaas.RequestIdentities.Infrastructure.Data;
using Vorn.Aaas.RequestIdentities.Presentation.Contracts;

namespace Vorn.Aaas.RequestIdentities.Application.Commands;

public sealed record RecordRequestIdentityCommand(
    string RequestId,
    string Authority,
    string ClientId,
    string OwnerId,
    string? TenantId,
    string? UserId,
    string? ResourceId,
    string? TraceId,
    string? AdditionalInfo) : IRequest<RequestIdentityDto>;

public sealed class RecordRequestIdentityCommandValidator : AbstractValidator<RecordRequestIdentityCommand>
{
    public RecordRequestIdentityCommandValidator()
    {
        _ = RuleFor(command => command.RequestId).NotEmpty();
        _ = RuleFor(command => command.Authority).NotEmpty();
        _ = RuleFor(command => command.ClientId).NotEmpty();
    }
}

internal sealed class RecordRequestIdentityCommandHandler(
    RequestIdentityDbContext dbContext,
    ITenantsService tenantsService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<RecordRequestIdentityCommandHandler> logger)
    : IRequestHandler<RecordRequestIdentityCommand, RequestIdentityDto>
{
    public async Task<RequestIdentityDto> Handle(RecordRequestIdentityCommand request, CancellationToken cancellationToken)
    {
        //if((request.ResourceId?.StartsWith("/api/users/") ?? false) ||
        //    (request.ResourceId?.StartsWith("/api/apiclients/") ?? false) ||
        //    (request.ResourceId?.StartsWith("/api/authenticationclients/") ?? false)
        //     || (request.ResourceId?.StartsWith("https://") ?? false)
        //     )
        //{
        //    return;
        //}

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

        RequestIdentity? entity = new()
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

        _ = await dbContext.RequestIdentities.AddAsync(entity, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return new RequestIdentityDto(entity.RequestId, entity.Authority, entity.ClientId, entity.OwnerId, entity.TenantId, entity.UserId, entity.ResourceId, entity.TraceId, entity.AdditionalInfo);
    }
}
