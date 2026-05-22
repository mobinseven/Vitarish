using MediatR;
using Vitarish.Appointments.Domain.Events;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Appointments.Infrastructure.EventHandlers;

internal sealed class ShiftBookingPolicyAuditEventHandlers(IAuditWriter auditWriter, IRequestContext requestContext)
    : INotificationHandler<ShiftBookingPolicyCreated>,
        INotificationHandler<ShiftBookingPolicyUpdated>,
        INotificationHandler<ShiftBookingPolicyRemoved>
{
    public Task Handle(ShiftBookingPolicyCreated notification, CancellationToken cancellationToken)
        => WriteAsync("Created", notification.PolicyId, cancellationToken);

    public Task Handle(ShiftBookingPolicyUpdated notification, CancellationToken cancellationToken)
        => WriteAsync("Updated", notification.PolicyId, cancellationToken);

    public Task Handle(ShiftBookingPolicyRemoved notification, CancellationToken cancellationToken)
        => WriteAsync("Removed", notification.PolicyId, cancellationToken);

    private Task WriteAsync(string action, Guid policyId, CancellationToken cancellationToken)
        => auditWriter.WriteAsync(
            new AuditEntry
            {
                Action = action,
                ActorId = requestContext.UserId,
                EntityType = nameof(ShiftBookingPolicy),
                EntityId = policyId,
                OccurredAt = DateTimeOffset.UtcNow,
                TenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID cannot be recognized.")
            },
            cancellationToken);
}
