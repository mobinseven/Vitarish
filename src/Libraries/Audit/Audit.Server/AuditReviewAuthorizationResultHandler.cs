using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Vitarish.Audit.Server;

// Marks only audit review endpoints; authorization itself remains unchanged.
internal sealed class AuditReviewEndpoint { }

internal sealed class AuditReviewAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

    public AuditReviewAuthorizationResultHandler() { }

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if ((authorizeResult.Challenged || authorizeResult.Forbidden) &&
            context.GetEndpoint()?.Metadata.GetMetadata<AuditReviewEndpoint>() is not null)
        {
            await context.RequestServices.GetRequiredService<AuditReviewDenialRecorder>()
                .RecordAsync(context).ConfigureAwait(false);
        }

        await defaultHandler.HandleAsync(next, context, policy, authorizeResult).ConfigureAwait(false);
    }
}
