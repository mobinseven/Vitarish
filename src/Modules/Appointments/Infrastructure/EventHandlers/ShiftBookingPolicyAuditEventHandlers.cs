using MediatR;
using Vitarish.Appointments.Domain.Events;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Appointments.Infrastructure.EventHandlers;

internal sealed class ShiftBookingPolicyCreatedAuditHandler(IAuditWriter auditWriter, IRequestContext requestContext) : INotificationHandler<ShiftBookingPolicyCreated>
{
    public Task Handle(ShiftBookingPolicyCreated notification, CancellationToken cancellationToken) => WriteAsync("Created", notification.PolicyId, cancellationToken);
    private Task WriteAsync(string action, Guid id, CancellationToken cancellationToken) => auditWriter.WriteAsync(new AuditEntry { Action = action, ActorId = requestContext.UserId, EntityType = nameof(ShiftBookingPolicy), EntityId = id, OccurredAt = DateTimeOffset.UtcNow, TenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID cannot be recognized.") }, cancellationToken);
}
internal sealed class ShiftBookingPolicyUpdatedAuditHandler(IAuditWriter auditWriter, IRequestContext requestContext) : INotificationHandler<ShiftBookingPolicyUpdated>
{
    public Task Handle(ShiftBookingPolicyUpdated notification, CancellationToken cancellationToken) => auditWriter.WriteAsync(new AuditEntry { Action = "Updated", ActorId = requestContext.UserId, EntityType = nameof(ShiftBookingPolicy), EntityId = notification.PolicyId, OccurredAt = DateTimeOffset.UtcNow, TenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID cannot be recognized.") }, cancellationToken);
}
internal sealed class ShiftBookingPolicyRemovedAuditHandler(IAuditWriter auditWriter, IRequestContext requestContext) : INotificationHandler<ShiftBookingPolicyRemoved>
{
    public Task Handle(ShiftBookingPolicyRemoved notification, CancellationToken cancellationToken) => auditWriter.WriteAsync(new AuditEntry { Action = "Removed", ActorId = requestContext.UserId, EntityType = nameof(ShiftBookingPolicy), EntityId = notification.PolicyId, OccurredAt = DateTimeOffset.UtcNow, TenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID cannot be recognized.") }, cancellationToken);
}
