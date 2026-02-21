using MediatR;
using Vitarish.Entities.Server.Infrastructure;
using Vitarish.Entities.Server.Models;
using Vitarish.Modules.Elasticsearch.Application.Models;

namespace Vitarish.Modules.Elasticsearch.Infrastructure.Services;

public sealed class EntityIndexingNotificationHandler(IEntityIndexingMessageQueue queue) : INotificationHandler<EntityNotification>
{
    public async Task Handle(EntityNotification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var args = notification.Args;
        var tenantId = NormalizeTenantId(args.CorrelationId);

        switch (args.Operation)
        {
            case EntityOperation.Created:
            case EntityOperation.Updated:
                await queue.EnqueueAsync(new EntityIndexed(
                    tenantId,
                    args.EntityName,
                    args.Id,
                    new EntityIndexingEnvelope(tenantId, args.EntityName, args.Id, args.OccurredAt, args.Snapshot, args.Changes),
                    args.OccurredAt), cancellationToken);
                break;

            case EntityOperation.Deleted:
                await queue.EnqueueAsync(new EntityDeleted(tenantId, args.EntityName, args.Id, args.OccurredAt), cancellationToken);
                break;
        }
    }

    private static string NormalizeTenantId(string? tenantId) => string.IsNullOrWhiteSpace(tenantId) ? "global" : tenantId.Trim();
}
